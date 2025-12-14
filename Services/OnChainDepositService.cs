using System;
using Wihngo.Data;
using Wihngo.Models.Entities;
using Wihngo.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Wihngo.Services;

public class OnChainDepositService : IOnChainDepositService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OnChainDepositService> _logger;

    public OnChainDepositService(
        AppDbContext context,
        ILogger<OnChainDepositService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OnChainDeposit> RecordDepositAsync(OnChainDeposit deposit)
    {
        try
        {
            // Check if transaction already exists
            var existing = await _context.OnChainDeposits
                .FirstOrDefaultAsync(d => d.TxHashOrSig == deposit.TxHashOrSig);

            if (existing != null)
            {
                _logger.LogWarning("Deposit with tx hash {TxHash} already exists", deposit.TxHashOrSig);
                return existing;
            }

            deposit.CreatedAt = DateTime.UtcNow;
            deposit.UpdatedAt = DateTime.UtcNow;
            deposit.DetectedAt = DateTime.UtcNow;

            _context.OnChainDeposits.Add(deposit);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Recorded new deposit: {Amount} {Token} on {Chain} for user {UserId}",
                deposit.AmountDecimal, deposit.Token, deposit.Chain, deposit.UserId);

            return deposit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording deposit for tx {TxHash}", deposit.TxHashOrSig);
            throw;
        }
    }

    public async Task<OnChainDeposit> UpdateDepositStatusAsync(Guid depositId, string status, int confirmations)
    {
        var deposit = await _context.OnChainDeposits.FindAsync(depositId);
        if (deposit == null)
        {
            throw new InvalidOperationException($"Deposit {depositId} not found");
        }

        deposit.Status = status;
        deposit.Confirmations = confirmations;
        deposit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated deposit {DepositId} status to {Status} with {Confirmations} confirmations",
            depositId, status, confirmations);

        return deposit;
    }

    public async Task<bool> CreditDepositToUserAsync(Guid depositId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var deposit = await _context.OnChainDeposits.FindAsync(depositId);
            if (deposit == null)
            {
                _logger.LogWarning("Deposit {DepositId} not found", depositId);
                return false;
            }

            if (deposit.Status == "credited")
            {
                _logger.LogWarning("Deposit {DepositId} already credited", depositId);
                return false;
            }

            var user = await _context.Users.FindAsync(deposit.UserId);
            if (user == null)
            {
                _logger.LogError("User {UserId} not found for deposit {DepositId}", deposit.UserId, depositId);
                return false;
            }

            // TODO: Implement balance crediting based on your business logic
            // Options:
            // 1. Create a wallet/balance table for users
            // 2. Create a transaction record for the credit
            // 3. Update a balance field on the User entity (requires adding the field)
            // 
            // For now, we just mark the deposit as credited without updating any balance
            // You should implement the actual crediting logic based on your requirements

            _logger.LogInformation(
                "Crediting {Amount} {Token} to user {UserId} - TODO: Implement actual balance update",
                deposit.AmountDecimal, deposit.Token, deposit.UserId);

            // Update deposit status
            deposit.Status = "credited";
            deposit.CreditedAt = DateTime.UtcNow;
            deposit.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Marked deposit {DepositId} as credited for user {UserId}",
                depositId, deposit.UserId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error crediting deposit {DepositId} to user", depositId);
            throw;
        }
    }

    public async Task<List<OnChainDeposit>> GetUserDepositsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _context.OnChainDeposits
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.DetectedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<OnChainDeposit>> GetPendingDepositsAsync()
    {
        return await _context.OnChainDeposits
            .Where(d => d.Status == "pending" || d.Status == "confirmed")
            .OrderBy(d => d.DetectedAt)
            .ToListAsync();
    }

    public async Task<bool> IsTransactionProcessedAsync(string txHashOrSig)
    {
        return await _context.OnChainDeposits
            .AnyAsync(d => d.TxHashOrSig == txHashOrSig);
    }

    public async Task<TokenConfiguration?> GetTokenConfigurationAsync(string token, string chain)
    {
        return await _context.TokenConfigurations
            .FirstOrDefaultAsync(t => t.Token == token && t.Chain == chain && t.IsActive);
    }

    public async Task<List<TokenConfiguration>> GetActiveTokenConfigurationsAsync()
    {
        return await _context.TokenConfigurations
            .Where(t => t.IsActive)
            .ToListAsync();
    }

    public async Task<string?> GetUserDerivedAddressAsync(Guid userId, string chain)
    {
        // This should be implemented based on your user address storage mechanism
        // For now, returning null - you'll need to implement this based on your requirements
        _logger.LogWarning("GetUserDerivedAddressAsync not fully implemented yet");
        return null;
    }

    public async Task<bool> RegisterUserAddressAsync(Guid userId, string chain, string address, string derivationPath)
    {
        // This should be implemented based on your user address storage mechanism
        // For now, returning false - you'll need to implement this based on your requirements
        _logger.LogWarning("RegisterUserAddressAsync not fully implemented yet");
        return await Task.FromResult(false);
    }
}
