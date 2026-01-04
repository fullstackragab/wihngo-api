using Microsoft.Extensions.Options;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;
using Wihngo.Configuration;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Service for building and submitting Solana transactions
/// </summary>
public class SolanaTransactionService : ISolanaTransactionService
{
    private readonly IRpcClient _rpcClient;
    private readonly P2PPaymentConfiguration _config;
    private readonly ISecretService _secretService;
    private readonly ILogger<SolanaTransactionService> _logger;

    // USDC has 6 decimals
    private const int USDC_DECIMALS = 6;

    // SPL Token Program ID
    private static readonly PublicKey TokenProgramId = new("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA");

    // Associated Token Program ID
    private static readonly PublicKey AssociatedTokenProgramId = new("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");

    public SolanaTransactionService(
        IOptions<P2PPaymentConfiguration> config,
        IOptions<SolanaConfig> solanaConfig,
        ISecretService secretService,
        ILogger<SolanaTransactionService> logger)
    {
        _config = config.Value;
        _secretService = secretService;
        _logger = logger;
        _rpcClient = ClientFactory.GetClient(solanaConfig.Value.RpcUrl);
    }

    /// <inheritdoc />
    public async Task<string> BuildUsdcTransferTransactionAsync(
        string senderPubkey,
        string recipientPubkey,
        decimal amountUsdc,
        string? feePayer = null,
        bool createRecipientAta = false,
        string? platformWalletPubkey = null,
        decimal platformFeeUsdc = 0)
    {
        try
        {
            var sender = new PublicKey(senderPubkey);
            var recipient = new PublicKey(recipientPubkey);
            var usdcMint = new PublicKey(_config.UsdcMintAddress);
            var feePayerKey = feePayer != null ? new PublicKey(feePayer) : sender;

            // Get ATAs for sender and recipient
            var senderAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(sender, usdcMint);
            var recipientAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(recipient, usdcMint);

            // Convert amounts to lamports (USDC has 6 decimals)
            var amountLamports = (ulong)(amountUsdc * (decimal)Math.Pow(10, USDC_DECIMALS));
            var platformFeeLamports = (ulong)(platformFeeUsdc * (decimal)Math.Pow(10, USDC_DECIMALS));

            // Get recent blockhash
            var blockHashResult = await _rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
            {
                throw new Exception($"Failed to get blockhash: {blockHashResult.Reason}");
            }

            var transactionBuilder = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(feePayerKey);

            // Create recipient ATA if needed
            if (createRecipientAta)
            {
                var createAtaIx = AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                    feePayerKey, // Payer for rent
                    recipient,   // Owner
                    usdcMint);   // Mint

                transactionBuilder.AddInstruction(createAtaIx);
            }

            // Add SPL Token transfer to bird owner
            var transferIx = TokenProgram.Transfer(
                senderAta,      // Source ATA
                recipientAta,   // Destination ATA
                amountLamports, // Amount to bird owner
                sender);        // Owner/Authority

            transactionBuilder.AddInstruction(transferIx);

            // Add platform fee transfer if applicable
            if (!string.IsNullOrEmpty(platformWalletPubkey) && platformFeeLamports > 0)
            {
                var platformWallet = new PublicKey(platformWalletPubkey);
                var platformAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(platformWallet, usdcMint);

                var platformTransferIx = TokenProgram.Transfer(
                    senderAta,           // Source ATA
                    platformAta,         // Platform ATA
                    platformFeeLamports, // Platform fee
                    sender);             // Owner/Authority

                transactionBuilder.AddInstruction(platformTransferIx);
            }

            // Compile the message to bytes (unsigned - client will sign)
            var messageBytes = transactionBuilder.CompileMessage();

            // For an unsigned transaction, we need to create the full transaction format:
            // [1 byte: num signatures] [64 bytes per signature (empty)] [message bytes]
            // If fee payer is different from sender, we need 2 signature slots:
            // - Slot 0: Fee payer (sponsor) signature
            // - Slot 1: Sender signature
            var hasSponsorship = feePayer != null && feePayer != senderPubkey;
            var numSignatures = hasSponsorship ? 2 : 1;
            var emptySignature = new byte[64]; // placeholder for signatures

            var transactionBytes = new byte[1 + (64 * numSignatures) + messageBytes.Length];
            transactionBytes[0] = (byte)numSignatures;

            // Leave signature slots empty (will be filled by signers)
            for (int i = 0; i < numSignatures; i++)
            {
                Array.Copy(emptySignature, 0, transactionBytes, 1 + (64 * i), 64);
            }

            Array.Copy(messageBytes, 0, transactionBytes, 1 + (64 * numSignatures), messageBytes.Length);

            // Serialize to base64 (ready for client to sign)
            var serialized = Convert.ToBase64String(transactionBytes);

            _logger.LogInformation(
                "Built USDC transfer transaction: {Amount} USDC to {Recipient}, PlatformFee: {Fee} USDC to {Platform}, FeePayer: {FeePayer}",
                amountUsdc, recipientPubkey, platformFeeUsdc, platformWalletPubkey ?? "none", feePayer ?? senderPubkey);

            return serialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build USDC transfer transaction");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> SubmitTransactionAsync(string signedTransactionBase64)
    {
        try
        {
            var txBytes = Convert.FromBase64String(signedTransactionBase64);
            var result = await _rpcClient.SendTransactionAsync(txBytes);

            if (!result.WasSuccessful)
            {
                _logger.LogError("Failed to submit transaction: {Error}", result.Reason);
                throw new Exception($"Transaction submission failed: {result.Reason}");
            }

            _logger.LogInformation("Transaction submitted successfully: {Signature}", result.Result);
            return result.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting transaction to Solana");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TransactionStatusResult> GetTransactionStatusAsync(string signature)
    {
        try
        {
            // First check if transaction is confirmed
            var statusResult = await _rpcClient.GetSignatureStatusesAsync(new List<string> { signature });

            if (!statusResult.WasSuccessful || statusResult.Result.Value == null || statusResult.Result.Value.Count == 0)
            {
                return new TransactionStatusResult
                {
                    Found = false,
                    Error = "Transaction not found"
                };
            }

            var status = statusResult.Result.Value[0];
            if (status == null)
            {
                return new TransactionStatusResult
                {
                    Found = false,
                    Error = "Transaction status is null"
                };
            }

            // Check for errors - if confirmation status is null and slot is 0, likely an error
            // The Solnet API doesn't expose error details on SignatureStatusInfo
            // A failed transaction would not reach finalized status

            // Get confirmation status
            var isFinalized = status.ConfirmationStatus == "finalized";
            var isConfirmed = status.ConfirmationStatus == "confirmed" || isFinalized;

            return new TransactionStatusResult
            {
                Found = true,
                Confirmed = isConfirmed,
                Finalized = isFinalized,
                Confirmations = (int)(status.Confirmations ?? (isFinalized ? (ulong)_config.RequiredConfirmations : 0)),
                Slot = (long)status.Slot
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction status for {Signature}", signature);
            return new TransactionStatusResult
            {
                Found = false,
                Error = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<decimal> GetSolBalanceAsync(string pubkey)
    {
        try
        {
            var publicKey = new PublicKey(pubkey);
            var result = await _rpcClient.GetBalanceAsync(publicKey);

            if (!result.WasSuccessful)
            {
                _logger.LogWarning("Failed to get SOL balance for {Pubkey}: {Error}", pubkey, result.Reason);
                return 0;
            }

            // Convert lamports to SOL (1 SOL = 10^9 lamports)
            return (decimal)result.Result.Value / 1_000_000_000m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SOL balance for {Pubkey}", pubkey);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<decimal> GetUsdcBalanceAsync(string pubkey)
    {
        try
        {
            var owner = new PublicKey(pubkey);
            var usdcMint = new PublicKey(_config.UsdcMintAddress);

            // Get ATA for this owner
            var ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner, usdcMint);

            var result = await _rpcClient.GetTokenAccountBalanceAsync(ata.Key);

            if (!result.WasSuccessful)
            {
                // Account might not exist
                _logger.LogDebug("No USDC ATA found for {Pubkey}", pubkey);
                return 0;
            }

            // Parse the UI amount
            if (decimal.TryParse(result.Result.Value.UiAmountString, out var balance))
            {
                return balance;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting USDC balance for {Pubkey}", pubkey);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckAtaExistsAsync(string ownerPubkey)
    {
        try
        {
            var owner = new PublicKey(ownerPubkey);
            var usdcMint = new PublicKey(_config.UsdcMintAddress);
            var ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner, usdcMint);

            var result = await _rpcClient.GetAccountInfoAsync(ata.Key);

            return result.WasSuccessful && result.Result.Value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ATA existence for {Pubkey}", ownerPubkey);
            return false;
        }
    }

    /// <inheritdoc />
    public Task<string> GetAtaAddressAsync(string ownerPubkey)
    {
        var owner = new PublicKey(ownerPubkey);
        var usdcMint = new PublicKey(_config.UsdcMintAddress);
        var ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner, usdcMint);

        return Task.FromResult(ata.Key);
    }

    /// <inheritdoc />
    public async Task<TransactionVerificationResult> VerifyTransactionAsync(
        string signature,
        string expectedSender,
        string expectedRecipient,
        decimal expectedAmount)
    {
        try
        {
            var result = await _rpcClient.GetTransactionAsync(signature, Commitment.Finalized);

            if (!result.WasSuccessful || result.Result == null)
            {
                return new TransactionVerificationResult
                {
                    Success = false,
                    Error = "Transaction not found or not finalized"
                };
            }

            var transaction = result.Result;

            // Check for errors - transaction failed on-chain if meta has error
            if (transaction.Meta?.Error != null)
            {
                return new TransactionVerificationResult
                {
                    Success = false,
                    Error = "Transaction failed on-chain"
                };
            }

            // For SPL token transfers, we need to look at the pre/post token balances
            var preBalances = transaction.Meta?.PreTokenBalances;
            var postBalances = transaction.Meta?.PostTokenBalances;

            if (preBalances == null || postBalances == null || postBalances.Length == 0)
            {
                return new TransactionVerificationResult
                {
                    Success = false,
                    Error = "No token balance changes found"
                };
            }

            // Find the USDC transfer
            var usdcMint = _config.UsdcMintAddress;
            decimal actualAmount = 0;
            bool senderFound = false;
            bool recipientFound = false;
            bool mintMatches = false;

            // Get account keys from the transaction for owner lookup
            var accountKeys = transaction.Transaction?.Message?.AccountKeys;

            // Check post balances for the recipient (increased)
            foreach (var balance in postBalances)
            {
                if (balance.Mint == usdcMint)
                {
                    mintMatches = true;
                    // Owner address from token balance - use account index to lookup
                    var ownerIndex = balance.AccountIndex;
                    var owner = accountKeys != null && ownerIndex < accountKeys.Length
                        ? accountKeys[ownerIndex]
                        : null;

                    if (owner == expectedRecipient)
                    {
                        recipientFound = true;
                        // Find the corresponding pre-balance to calculate the difference
                        var preBal = preBalances.FirstOrDefault(b => b.AccountIndex == balance.AccountIndex);
                        if (preBal != null && decimal.TryParse(balance.UiTokenAmount?.UiAmountString, out var post)
                            && decimal.TryParse(preBal.UiTokenAmount?.UiAmountString, out var pre))
                        {
                            actualAmount = post - pre;
                        }
                        else if (decimal.TryParse(balance.UiTokenAmount?.UiAmountString, out var postOnly))
                        {
                            // New ATA case
                            actualAmount = postOnly;
                        }
                    }

                    if (owner == expectedSender)
                    {
                        senderFound = true;
                    }
                }
            }

            // Also check pre-balances for sender
            foreach (var balance in preBalances)
            {
                var ownerIndex = balance.AccountIndex;
                var owner = accountKeys != null && ownerIndex < accountKeys.Length
                    ? accountKeys[ownerIndex]
                    : null;
                if (balance.Mint == usdcMint && owner == expectedSender)
                {
                    senderFound = true;
                }
            }

            // SECURITY: Require exact amount match (no tolerance) to prevent underpayment attacks
            // Both values are in USDC with 6 decimal precision
            var amountMatches = actualAmount == expectedAmount;

            return new TransactionVerificationResult
            {
                Success = senderFound && recipientFound && amountMatches && mintMatches,
                AmountMatches = amountMatches,
                SenderMatches = senderFound,
                RecipientMatches = recipientFound,
                MintMatches = mintMatches,
                ActualAmount = actualAmount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying transaction {Signature}", signature);
            return new TransactionVerificationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<DualTransferVerificationResult> VerifyDualTransferTransactionAsync(
        string signature,
        string expectedSender,
        string expectedBirdWallet,
        decimal expectedBirdAmount,
        string expectedWihngoWallet,
        decimal expectedWihngoAmount)
    {
        try
        {
            // Fetch transaction with finalized commitment
            var result = await _rpcClient.GetTransactionAsync(signature, Commitment.Finalized);

            if (!result.WasSuccessful || result.Result == null)
            {
                return new DualTransferVerificationResult
                {
                    Success = false,
                    TransactionFound = false,
                    Error = "Transaction not found or not finalized"
                };
            }

            var transaction = result.Result;

            // Check for on-chain errors
            if (transaction.Meta?.Error != null)
            {
                return new DualTransferVerificationResult
                {
                    Success = false,
                    TransactionFound = true,
                    TransactionSucceeded = false,
                    Error = "Transaction failed on-chain"
                };
            }

            // Get account keys for payer extraction
            var accountKeys = transaction.Transaction?.Message?.AccountKeys;
            string? actualPayer = accountKeys?.Length > 0 ? accountKeys[0] : null;
            bool payerMatches = actualPayer != null && actualPayer == expectedSender;

            // Parse pre/post token balances to find USDC transfers
            var preBalances = transaction.Meta?.PreTokenBalances;
            var postBalances = transaction.Meta?.PostTokenBalances;

            if (preBalances == null || postBalances == null)
            {
                return new DualTransferVerificationResult
                {
                    Success = false,
                    TransactionFound = true,
                    TransactionSucceeded = true,
                    Error = "No token balance changes found in transaction"
                };
            }

            var usdcMint = _config.UsdcMintAddress;
            bool mintMatches = false;

            // Build a map of token account changes
            var tokenChanges = new Dictionary<int, (string? Owner, decimal PreBalance, decimal PostBalance)>();

            // Process post balances
            foreach (var balance in postBalances)
            {
                if (balance.Mint == usdcMint)
                {
                    mintMatches = true;
                    var accountIndex = balance.AccountIndex;

                    // Get owner from the account keys if available
                    // For SPL token accounts, we need to use the owner field from the balance
                    decimal postAmount = 0;
                    if (decimal.TryParse(balance.UiTokenAmount?.UiAmountString, out var parsed))
                    {
                        postAmount = parsed;
                    }

                    if (!tokenChanges.ContainsKey(accountIndex))
                    {
                        tokenChanges[accountIndex] = (balance.Owner, 0, postAmount);
                    }
                    else
                    {
                        var existing = tokenChanges[accountIndex];
                        tokenChanges[accountIndex] = (balance.Owner ?? existing.Owner, existing.PreBalance, postAmount);
                    }
                }
            }

            // Process pre balances
            foreach (var balance in preBalances)
            {
                if (balance.Mint == usdcMint)
                {
                    mintMatches = true;
                    var accountIndex = balance.AccountIndex;

                    decimal preAmount = 0;
                    if (decimal.TryParse(balance.UiTokenAmount?.UiAmountString, out var parsed))
                    {
                        preAmount = parsed;
                    }

                    if (!tokenChanges.ContainsKey(accountIndex))
                    {
                        tokenChanges[accountIndex] = (balance.Owner, preAmount, 0);
                    }
                    else
                    {
                        var existing = tokenChanges[accountIndex];
                        tokenChanges[accountIndex] = (balance.Owner ?? existing.Owner, preAmount, existing.PostBalance);
                    }
                }
            }

            // Find transfers (accounts where balance increased)
            var transfers = new List<(string Owner, decimal Amount)>();
            foreach (var kvp in tokenChanges)
            {
                var (owner, pre, post) = kvp.Value;
                if (owner != null && post > pre)
                {
                    transfers.Add((owner, post - pre));
                }
            }

            // Count USDC transfers
            int usdcTransferCount = transfers.Count;

            // Initialize transfer details
            TransferDetail? birdTransfer = null;
            TransferDetail? wihngoTransfer = null;

            // Find bird transfer
            var birdMatch = transfers.FirstOrDefault(t =>
                t.Owner.Equals(expectedBirdWallet, StringComparison.OrdinalIgnoreCase));

            if (birdMatch.Owner != null)
            {
                birdTransfer = new TransferDetail
                {
                    Found = true,
                    Destination = birdMatch.Owner,
                    ExpectedAmount = expectedBirdAmount,
                    ActualAmount = birdMatch.Amount,
                    AmountMatches = birdMatch.Amount == expectedBirdAmount,
                    DestinationMatches = true
                };
            }
            else
            {
                birdTransfer = new TransferDetail
                {
                    Found = false,
                    Destination = expectedBirdWallet,
                    ExpectedAmount = expectedBirdAmount,
                    ActualAmount = 0,
                    AmountMatches = false,
                    DestinationMatches = false
                };
            }

            // Find Wihngo transfer
            var wihngoMatch = transfers.FirstOrDefault(t =>
                t.Owner.Equals(expectedWihngoWallet, StringComparison.OrdinalIgnoreCase));

            if (wihngoMatch.Owner != null)
            {
                wihngoTransfer = new TransferDetail
                {
                    Found = true,
                    Destination = wihngoMatch.Owner,
                    ExpectedAmount = expectedWihngoAmount,
                    ActualAmount = wihngoMatch.Amount,
                    AmountMatches = wihngoMatch.Amount == expectedWihngoAmount,
                    DestinationMatches = true
                };
            }
            else
            {
                wihngoTransfer = new TransferDetail
                {
                    Found = false,
                    Destination = expectedWihngoWallet,
                    ExpectedAmount = expectedWihngoAmount,
                    ActualAmount = 0,
                    AmountMatches = false,
                    DestinationMatches = false
                };
            }

            // Determine overall success
            // Both transfers must be found with exact amounts, correct destinations, and payer must match
            bool success = mintMatches &&
                           payerMatches &&
                           usdcTransferCount == 2 &&
                           birdTransfer.Found &&
                           birdTransfer.AmountMatches &&
                           wihngoTransfer.Found &&
                           wihngoTransfer.AmountMatches;

            // Build error message if not successful
            string? error = null;
            if (!success)
            {
                var errors = new List<string>();
                if (!mintMatches) errors.Add("Wrong token mint (not USDC)");
                if (!payerMatches) errors.Add($"Payer mismatch: expected {expectedSender}, got {actualPayer}");
                if (usdcTransferCount != 2) errors.Add($"Expected 2 USDC transfers, found {usdcTransferCount}");
                if (!birdTransfer.Found) errors.Add("Bird transfer not found");
                else if (!birdTransfer.AmountMatches) errors.Add($"Bird amount mismatch: expected {expectedBirdAmount}, got {birdTransfer.ActualAmount}");
                if (!wihngoTransfer.Found) errors.Add("Wihngo transfer not found");
                else if (!wihngoTransfer.AmountMatches) errors.Add($"Wihngo amount mismatch: expected {expectedWihngoAmount}, got {wihngoTransfer.ActualAmount}");
                error = string.Join("; ", errors);
            }

            _logger.LogInformation(
                "Dual transfer verification: Signature={Signature}, Success={Success}, BirdTransfer={BirdFound}/{BirdAmount}, WihngoTransfer={WihngoFound}/{WihngoAmount}",
                signature, success,
                birdTransfer.Found, birdTransfer.ActualAmount,
                wihngoTransfer.Found, wihngoTransfer.ActualAmount);

            return new DualTransferVerificationResult
            {
                Success = success,
                TransactionFound = true,
                TransactionSucceeded = true,
                MintMatches = mintMatches,
                BirdTransfer = birdTransfer,
                WihngoTransfer = wihngoTransfer,
                ActualPayer = actualPayer,
                PayerMatches = payerMatches,
                UsdcTransferCount = usdcTransferCount,
                Error = error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying dual transfer transaction {Signature}", signature);
            return new DualTransferVerificationResult
            {
                Success = false,
                TransactionFound = false,
                Error = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<string> AddSponsorSignatureAsync(string partiallySignedTransactionBase64)
    {
        try
        {
            // Get sponsor wallet secret key from secure storage
            // This should be a base58-encoded 64-byte secret key (private key + public key)
            var sponsorSecretKeyBase58 = await _secretService.GetRequiredSecretAsync("SponsorWalletPrivateKey");

            // Decode the base58 secret key to bytes
            // Solana secret keys are 64 bytes: first 32 = private key, last 32 = public key
            byte[] secretKeyBytes;
            try
            {
                secretKeyBytes = Encoders.Base58.DecodeData(sponsorSecretKeyBase58);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid base58 format for sponsor wallet secret key", ex);
            }

            if (secretKeyBytes.Length != 64)
            {
                throw new InvalidOperationException(
                    $"Sponsor wallet secret key must be 64 bytes, got {secretKeyBytes.Length}. " +
                    "Ensure you're using the full secret key (not just the private key seed).");
            }

            // Create account from secret key bytes
            // privateKey = first 32 bytes, publicKey = last 32 bytes
            var privateKey = secretKeyBytes[..32];
            var publicKey = secretKeyBytes[32..];
            var sponsorAccount = new Account(privateKey, publicKey);

            // Verify the sponsor public key matches configuration
            var expectedSponsorPubkey = _config.GasSponsorship?.SponsorWalletPubkey;
            if (expectedSponsorPubkey != null && sponsorAccount.PublicKey.Key != expectedSponsorPubkey)
            {
                _logger.LogError(
                    "Sponsor wallet mismatch: expected {Expected}, got {Actual}",
                    expectedSponsorPubkey, sponsorAccount.PublicKey.Key);
                throw new InvalidOperationException("Sponsor wallet private key does not match configured public key");
            }

            // Decode the transaction
            var txBytes = Convert.FromBase64String(partiallySignedTransactionBase64);

            // Parse transaction structure:
            // [1 byte: num signatures] [64 bytes per signature] [message bytes]
            var numSignatures = txBytes[0];

            if (numSignatures < 2)
            {
                throw new InvalidOperationException(
                    $"Transaction has {numSignatures} signature slot(s), expected at least 2 for sponsored transaction");
            }

            // Extract the message bytes (everything after signatures)
            var messageStart = 1 + (64 * numSignatures);
            var messageBytes = new byte[txBytes.Length - messageStart];
            Array.Copy(txBytes, messageStart, messageBytes, 0, messageBytes.Length);

            // Sign the message with sponsor wallet
            // The sponsor (fee payer) signature goes in slot 0
            var sponsorSignature = sponsorAccount.Sign(messageBytes);

            // Copy sponsor signature to slot 0
            Array.Copy(sponsorSignature, 0, txBytes, 1, 64);

            _logger.LogInformation(
                "Added sponsor signature from wallet {SponsorPubkey}",
                sponsorAccount.PublicKey.Key);

            return Convert.ToBase64String(txBytes);
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw configuration errors
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding sponsor signature to transaction");
            throw new InvalidOperationException("Failed to add sponsor signature: " + ex.Message, ex);
        }
    }
}
