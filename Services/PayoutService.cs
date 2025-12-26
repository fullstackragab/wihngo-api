using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Wihngo.Data;
using Wihngo.Dtos;
using Wihngo.Models.Enums;
using Wihngo.Models.Payout;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services
{
    public class PayoutService : IPayoutService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly ILogger<PayoutService> _logger;
        private readonly IPayoutValidationService _validationService;
        private readonly IPayoutCalculationService _calculationService;
        private readonly IWisePayoutService _wiseService;
        private readonly IPayPalPayoutService _paypalService;

        public PayoutService(
            IDbConnectionFactory dbFactory,
            ILogger<PayoutService> logger,
            IPayoutValidationService validationService,
            IPayoutCalculationService calculationService,
            IWisePayoutService wiseService,
            IPayPalPayoutService paypalService)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _validationService = validationService;
            _calculationService = calculationService;
            _wiseService = wiseService;
            _paypalService = paypalService;
        }

        public async Task<PayoutBalanceDto?> GetBalanceAsync(Guid userId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get payout balance
            var balance = await connection.QueryFirstOrDefaultAsync<PayoutBalance>(@"
                SELECT * FROM payout_balances WHERE user_id = @UserId",
                new { UserId = userId });

            if (balance == null)
            {
                // Create balance record if it doesn't exist
                await connection.ExecuteAsync(@"
                    INSERT INTO payout_balances (user_id, available_balance, pending_balance, next_payout_date, updated_at)
                    VALUES (@UserId, 0, 0, @NextPayoutDate, @UpdatedAt)
                    ON CONFLICT (user_id) DO NOTHING",
                    new
                    {
                        UserId = userId,
                        NextPayoutDate = GetNextPayoutDate(),
                        UpdatedAt = DateTime.UtcNow
                    });

                balance = new PayoutBalance { UserId = userId };
            }

            // Get earnings summary
            var earnings = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT * FROM get_bird_owner_earnings(@UserId)",
                new { UserId = userId });

            return new PayoutBalanceDto
            {
                UserId = userId,
                AvailableBalance = balance.AvailableBalance,
                PendingBalance = balance.PendingBalance,
                Currency = balance.Currency,
                NextPayoutDate = balance.NextPayoutDate,
                MinimumReached = balance.MinimumReached,
                Summary = new PayoutSummaryDto
                {
                    TotalEarned = earnings?.total_earned ?? 0,
                    TotalPaidOut = earnings?.total_paid_out ?? 0,
                    PlatformFeePaid = earnings?.platform_fee_paid ?? 0,
                    ProviderFeesPaid = earnings?.provider_fees_paid ?? 0
                }
            };
        }

        public async Task<List<PayoutMethodDto>> GetPayoutMethodsAsync(Guid userId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var methods = await connection.QueryAsync<PayoutMethod>(@"
                SELECT * FROM payout_methods 
                WHERE user_id = @UserId 
                ORDER BY is_default DESC, created_at DESC",
                new { UserId = userId });

            return methods.Select(MapToDto).ToList();
        }

        public async Task<PayoutMethodDto?> GetPayoutMethodByIdAsync(Guid methodId, Guid userId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var method = await connection.QueryFirstOrDefaultAsync<PayoutMethod>(@"
                SELECT * FROM payout_methods 
                WHERE id = @Id AND user_id = @UserId",
                new { Id = methodId, UserId = userId });

            return method != null ? MapToDto(method) : null;
        }

        public async Task<PayoutMethodDto> AddPayoutMethodAsync(Guid userId, PayoutMethodCreateDto dto)
        {
            // Parse method type
            if (!Enum.TryParse<PayoutMethodType>(dto.MethodType, true, out var methodType))
            {
                throw new ArgumentException($"Invalid method type: {dto.MethodType}");
            }

            // Validate based on method type
            var validationResult = await ValidateMethodAsync(methodType, dto);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException(validationResult.Error);
            }

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check if this is the first method
            var existingCount = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM payout_methods WHERE user_id = @UserId",
                new { UserId = userId });

            var isFirstMethod = existingCount == 0;
            var isDefault = dto.IsDefault || isFirstMethod;

            // Map method type to string for database
            var methodTypeStr = MapMethodTypeToString(methodType);

            // Insert new method
            var methodId = await connection.ExecuteScalarAsync<Guid>(@"
                INSERT INTO payout_methods (
                    user_id, method_type, is_default, is_verified,
                    account_holder_name, iban, bic, bank_name,
                    paypal_email,
                    wallet_address, network, currency,
                    created_at, updated_at
                ) VALUES (
                    @UserId, @MethodType, @IsDefault, FALSE,
                    @AccountHolderName, @Iban, @Bic, @BankName,
                    @PayPalEmail,
                    @WalletAddress, @Network, @Currency,
                    @CreatedAt, @UpdatedAt
                ) RETURNING id",
                new
                {
                    UserId = userId,
                    MethodType = methodTypeStr,
                    IsDefault = isDefault,
                    AccountHolderName = dto.AccountHolderName,
                    Iban = dto.Iban?.ToUpperInvariant().Replace(" ", ""),
                    Bic = dto.Bic?.ToUpperInvariant(),
                    BankName = dto.BankName,
                    PayPalEmail = dto.PayPalEmail,
                    WalletAddress = dto.WalletAddress,
                    Network = dto.Network,
                    Currency = dto.Currency,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

            var method = await connection.QueryFirstAsync<PayoutMethod>(@"
                SELECT * FROM payout_methods WHERE id = @Id",
                new { Id = methodId });

            _logger.LogInformation("Added payout method {MethodId} for user {UserId}", methodId, userId);

            return MapToDto(method);
        }

        public async Task<PayoutMethodDto?> UpdatePayoutMethodAsync(
            Guid methodId, 
            Guid userId, 
            PayoutMethodUpdateDto dto)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check ownership
            var method = await connection.QueryFirstOrDefaultAsync<PayoutMethod>(@"
                SELECT * FROM payout_methods 
                WHERE id = @Id AND user_id = @UserId",
                new { Id = methodId, UserId = userId });

            if (method == null)
                return null;

            // Update fields
            if (dto.IsDefault.HasValue)
            {
                await connection.ExecuteAsync(@"
                    UPDATE payout_methods 
                    SET is_default = @IsDefault, updated_at = @UpdatedAt 
                    WHERE id = @Id",
                    new
                    {
                        Id = methodId,
                        IsDefault = dto.IsDefault.Value,
                        UpdatedAt = DateTime.UtcNow
                    });
            }

            // Fetch updated method
            method = await connection.QueryFirstAsync<PayoutMethod>(@"
                SELECT * FROM payout_methods WHERE id = @Id",
                new { Id = methodId });

            _logger.LogInformation("Updated payout method {MethodId} for user {UserId}", methodId, userId);

            return MapToDto(method);
        }

        public async Task<bool> DeletePayoutMethodAsync(Guid methodId, Guid userId)
        {
            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Check ownership and get method details
            var method = await connection.QueryFirstOrDefaultAsync<PayoutMethod>(@"
                SELECT * FROM payout_methods 
                WHERE id = @Id AND user_id = @UserId",
                new { Id = methodId, UserId = userId });

            if (method == null)
                return false;

            // Check if there are pending payouts using this method
            var hasPendingPayouts = await connection.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS(
                    SELECT 1 FROM payout_transactions 
                    WHERE payout_method_id = @MethodId AND status IN ('pending', 'processing')
                )",
                new { MethodId = methodId });

            if (hasPendingPayouts)
            {
                throw new InvalidOperationException("Cannot delete method with pending payouts");
            }

            // Check if this is the only method and user has balance
            var methodCount = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM payout_methods WHERE user_id = @UserId",
                new { UserId = userId });

            var balance = await connection.ExecuteScalarAsync<decimal>(@"
                SELECT COALESCE(available_balance, 0) FROM payout_balances WHERE user_id = @UserId",
                new { UserId = userId });

            if (methodCount == 1 && balance > 0)
            {
                throw new InvalidOperationException("Cannot delete only payment method when balance exists");
            }

            // Delete the method
            await connection.ExecuteAsync(@"
                DELETE FROM payout_methods WHERE id = @Id",
                new { Id = methodId });

            // If deleted method was default, set another as default
            if (method.IsDefault && methodCount > 1)
            {
                await connection.ExecuteAsync(@"
                    UPDATE payout_methods 
                    SET is_default = TRUE, updated_at = @UpdatedAt 
                    WHERE user_id = @UserId AND id != @DeletedId
                    LIMIT 1",
                    new
                    {
                        UserId = userId,
                        DeletedId = methodId,
                        UpdatedAt = DateTime.UtcNow
                    });
            }

            _logger.LogInformation("Deleted payout method {MethodId} for user {UserId}", methodId, userId);

            return true;
        }

        public async Task<PayoutHistoryResponseDto> GetPayoutHistoryAsync(
            Guid userId,
            int page = 1,
            int pageSize = 20,
            string? status = null)
        {
            pageSize = Math.Min(pageSize, 100); // Max 100 items per page
            var offset = (page - 1) * pageSize;

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            var whereClause = "WHERE user_id = @UserId";
            if (!string.IsNullOrEmpty(status))
            {
                whereClause += " AND status = @Status";
            }

            // Get total count
            var totalCount = await connection.ExecuteScalarAsync<int>($@"
                SELECT COUNT(*) FROM payout_transactions {whereClause}",
                new { UserId = userId, Status = status });

            // Get paginated results
            var transactions = await connection.QueryAsync<dynamic>($@"
                SELECT 
                    pt.*,
                    pm.method_type
                FROM payout_transactions pt
                JOIN payout_methods pm ON pm.id = pt.payout_method_id
                {whereClause}
                ORDER BY pt.created_at DESC
                LIMIT @PageSize OFFSET @Offset",
                new
                {
                    UserId = userId,
                    Status = status,
                    PageSize = pageSize,
                    Offset = offset
                });

            var items = transactions.Select(t => new PayoutTransactionDto
            {
                Id = t.id,
                UserId = t.user_id,
                PayoutMethodId = t.payout_method_id,
                Amount = t.amount,
                Currency = t.currency,
                Status = t.status,
                MethodType = MapStringToMethodType(t.method_type),
                PlatformFee = t.platform_fee,
                ProviderFee = t.provider_fee,
                NetAmount = t.net_amount,
                ScheduledAt = t.scheduled_at,
                ProcessedAt = t.processed_at,
                CompletedAt = t.completed_at,
                FailureReason = t.failure_reason,
                TransactionId = t.transaction_id,
                CreatedAt = t.created_at
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PayoutHistoryResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = items
            };
        }

        public async Task<ProcessPayoutResponseDto> ProcessPayoutsAsync(Guid? userId = null)
        {
            var result = new ProcessPayoutResponseDto();

            using var connection = await _dbFactory.CreateOpenConnectionAsync();

            // Get users eligible for payout
            var eligibleUsers = await connection.QueryAsync<dynamic>(@"
                SELECT pb.user_id, pb.available_balance, pm.id as method_id, pm.method_type
                FROM payout_balances pb
                JOIN payout_methods pm ON pm.user_id = pb.user_id AND pm.is_default = TRUE
                WHERE pb.available_balance >= 20.00
                  AND (@UserId IS NULL OR pb.user_id = @UserId)",
                new { UserId = userId });

            foreach (var user in eligibleUsers)
            {
                result.Processed++;
                var detail = await ProcessSinglePayoutAsync(
                    connection,
                    user.user_id,
                    user.available_balance,
                    user.method_id,
                    user.method_type);

                result.Details.Add(detail);

                if (detail.Status == "completed")
                {
                    result.Successful++;
                    result.TotalAmount += detail.Amount;
                }
                else
                {
                    result.Failed++;
                }
            }

            _logger.LogInformation("Processed {Processed} payouts: {Successful} successful, {Failed} failed",
                result.Processed, result.Successful, result.Failed);

            return result;
        }

        // Helper methods

        private async Task<PayoutProcessDetailDto> ProcessSinglePayoutAsync(
            System.Data.Common.DbConnection connection,
            Guid userId,
            decimal amount,
            Guid methodId,
            string methodType)
        {
            var detail = new PayoutProcessDetailDto
            {
                UserId = userId,
                Amount = amount
            };

            try
            {
                // Calculate fees
                var platformFee = _calculationService.CalculatePlatformFee(amount);
                var providerFee = _calculationService.CalculateProviderFee(amount, methodType);
                var netAmount = _calculationService.CalculateNetAmount(amount, platformFee, providerFee);

                // Create transaction record
                var transactionId = await connection.ExecuteScalarAsync<Guid>(@"
                    INSERT INTO payout_transactions (
                        user_id, payout_method_id, amount, currency, status,
                        platform_fee, provider_fee, net_amount,
                        scheduled_at, created_at, updated_at
                    ) VALUES (
                        @UserId, @MethodId, @Amount, 'EUR', 'pending',
                        @PlatformFee, @ProviderFee, @NetAmount,
                        @ScheduledAt, @CreatedAt, @UpdatedAt
                    ) RETURNING id",
                    new
                    {
                        UserId = userId,
                        MethodId = methodId,
                        Amount = amount,
                        PlatformFee = platformFee,
                        ProviderFee = providerFee,
                        NetAmount = netAmount,
                        ScheduledAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });

                // Get method details
                var method = await connection.QueryFirstAsync<PayoutMethod>(@"
                    SELECT * FROM payout_methods WHERE id = @Id",
                    new { Id = methodId });

                // Process payout via appropriate provider
                var result = await ProcessViaProviderAsync(method, netAmount);

                if (result.Success)
                {
                    // Update transaction as completed
                    await connection.ExecuteAsync(@"
                        UPDATE payout_transactions 
                        SET status = 'completed', 
                            transaction_id = @TransactionId,
                            processed_at = @ProcessedAt,
                            completed_at = @CompletedAt,
                            updated_at = @UpdatedAt
                        WHERE id = @Id",
                        new
                        {
                            Id = transactionId,
                            TransactionId = result.TransactionId,
                            ProcessedAt = DateTime.UtcNow,
                            CompletedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });

                    // Deduct from balance
                    await connection.ExecuteAsync(@"
                        UPDATE payout_balances 
                        SET available_balance = available_balance - @Amount,
                            last_payout_date = @PayoutDate,
                            updated_at = @UpdatedAt
                        WHERE user_id = @UserId",
                        new
                        {
                            UserId = userId,
                            Amount = amount,
                            PayoutDate = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });

                    detail.Status = "completed";
                    detail.TransactionId = result.TransactionId;
                }
                else
                {
                    // Update transaction as failed
                    await connection.ExecuteAsync(@"
                        UPDATE payout_transactions 
                        SET status = 'failed', 
                            failure_reason = @FailureReason,
                            processed_at = @ProcessedAt,
                            updated_at = @UpdatedAt
                        WHERE id = @Id",
                        new
                        {
                            Id = transactionId,
                            FailureReason = result.Error,
                            ProcessedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });

                    detail.Status = "failed";
                    detail.Error = result.Error;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payout for user {UserId}", userId);
                detail.Status = "failed";
                detail.Error = ex.Message;
            }

            return detail;
        }

        private async Task<(bool Success, string? TransactionId, string? Error)> ProcessViaProviderAsync(
            PayoutMethod method,
            decimal amount)
        {
            return method.MethodType switch
            {
                PayoutMethodType.Iban => await _wiseService.ProcessPayoutAsync(
                    method.Iban!,
                    method.Bic!,
                    method.AccountHolderName!,
                    amount,
                    $"Wihngo Earnings - {DateTime.UtcNow:yyyy-MM}"),

                PayoutMethodType.PayPal => await _paypalService.ProcessPayoutAsync(
                    method.PayPalEmail!,
                    amount,
                    $"Wihngo earnings payout"),

                // Crypto payouts temporarily disabled - use P2P payment system instead
                PayoutMethodType.UsdcSolana => (false, null, "USDC Solana payouts are temporarily unavailable"),
                PayoutMethodType.EurcSolana => (false, null, "EURC Solana payouts are temporarily unavailable"),
                PayoutMethodType.UsdcBase => (false, null, "USDC Base payouts are temporarily unavailable"),
                PayoutMethodType.EurcBase => (false, null, "EURC Base payouts are temporarily unavailable"),

                _ => (false, null, "Unsupported payment method")
            };
        }

        private async Task<(bool IsValid, string? Error)> ValidateMethodAsync(
            PayoutMethodType methodType,
            PayoutMethodCreateDto dto)
        {
            return methodType switch
            {
                PayoutMethodType.Iban => await _validationService.ValidateIbanAsync(dto.Iban ?? ""),
                PayoutMethodType.PayPal => await _validationService.ValidatePayPalEmailAsync(dto.PayPalEmail ?? ""),
                PayoutMethodType.UsdcSolana or PayoutMethodType.EurcSolana =>
                    await _validationService.ValidateSolanaAddressAsync(dto.WalletAddress ?? ""),
                PayoutMethodType.UsdcBase or PayoutMethodType.EurcBase =>
                    await _validationService.ValidateBaseAddressAsync(dto.WalletAddress ?? ""),
                _ => (false, "Invalid method type")
            };
        }

        private PayoutMethodDto MapToDto(PayoutMethod method)
        {
            return new PayoutMethodDto
            {
                Id = method.Id,
                UserId = method.UserId,
                MethodType = MapMethodTypeToString(method.MethodType),
                IsDefault = method.IsDefault,
                IsVerified = method.IsVerified,
                CreatedAt = method.CreatedAt,
                UpdatedAt = method.UpdatedAt,
                AccountHolderName = method.AccountHolderName,
                Iban = method.GetMaskedIban(),
                Bic = method.Bic,
                BankName = method.BankName,
                PayPalEmail = method.PayPalEmail,
                WalletAddress = method.GetMaskedWalletAddress(),
                Network = method.Network,
                Currency = method.Currency
            };
        }

        private string MapMethodTypeToString(PayoutMethodType methodType)
        {
            return methodType switch
            {
                PayoutMethodType.Iban => "iban",
                PayoutMethodType.PayPal => "paypal",
                PayoutMethodType.UsdcSolana => "usdc-solana",
                PayoutMethodType.EurcSolana => "eurc-solana",
                PayoutMethodType.UsdcBase => "usdc-base",
                PayoutMethodType.EurcBase => "eurc-base",
                _ => "unknown"
            };
        }

        private string MapStringToMethodType(string methodType)
        {
            return methodType?.ToLowerInvariant() switch
            {
                "iban" => "iban",
                "paypal" => "paypal",
                "usdc-solana" => "usdc-solana",
                "eurc-solana" => "eurc-solana",
                "usdc-base" => "usdc-base",
                "eurc-base" => "eurc-base",
                _ => "unknown"
            };
        }

        private DateTime GetNextPayoutDate()
        {
            var now = DateTime.UtcNow;
            var firstOfNextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            return firstOfNextMonth;
        }
    }
}
