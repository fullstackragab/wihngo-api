namespace Wihngo.Services.Interfaces;

/// <summary>
/// Service for building and submitting Solana transactions
/// </summary>
public interface ISolanaTransactionService
{
    /// <summary>
    /// Builds an unsigned USDC SPL token transfer transaction
    /// </summary>
    /// <param name="senderPubkey">Sender's wallet public key</param>
    /// <param name="recipientPubkey">Recipient's wallet public key (bird owner)</param>
    /// <param name="amountUsdc">Amount of USDC to transfer to recipient</param>
    /// <param name="feePayer">Optional fee payer (platform wallet if sponsoring gas)</param>
    /// <param name="createRecipientAta">Whether to include ATA creation for recipient</param>
    /// <param name="platformWalletPubkey">Optional platform wallet for fee transfer</param>
    /// <param name="platformFeeUsdc">Optional platform fee amount in USDC</param>
    /// <returns>Base64 encoded unsigned transaction</returns>
    Task<string> BuildUsdcTransferTransactionAsync(
        string senderPubkey,
        string recipientPubkey,
        decimal amountUsdc,
        string? feePayer = null,
        bool createRecipientAta = false,
        string? platformWalletPubkey = null,
        decimal platformFeeUsdc = 0);

    /// <summary>
    /// Submits a signed transaction to Solana
    /// </summary>
    /// <param name="signedTransactionBase64">Base64 encoded signed transaction</param>
    /// <returns>Transaction signature</returns>
    Task<string> SubmitTransactionAsync(string signedTransactionBase64);

    /// <summary>
    /// Gets the status and confirmation count of a transaction
    /// </summary>
    Task<TransactionStatusResult> GetTransactionStatusAsync(string signature);

    /// <summary>
    /// Gets SOL balance of a wallet
    /// </summary>
    Task<decimal> GetSolBalanceAsync(string pubkey);

    /// <summary>
    /// Gets USDC balance of a wallet
    /// </summary>
    Task<decimal> GetUsdcBalanceAsync(string pubkey);

    /// <summary>
    /// Checks if the wallet has a USDC Associated Token Account
    /// </summary>
    Task<bool> CheckAtaExistsAsync(string ownerPubkey);

    /// <summary>
    /// Gets the USDC ATA address for a wallet
    /// </summary>
    Task<string> GetAtaAddressAsync(string ownerPubkey);

    /// <summary>
    /// Verifies that a transaction matches expected parameters
    /// </summary>
    Task<TransactionVerificationResult> VerifyTransactionAsync(
        string signature,
        string expectedSender,
        string expectedRecipient,
        decimal expectedAmount);

    /// <summary>
    /// Adds the sponsor wallet signature to a partially signed transaction
    /// Required for gas-sponsored transactions where the platform wallet is the fee payer
    /// </summary>
    /// <param name="partiallySignedTransactionBase64">Base64 encoded transaction with user signature</param>
    /// <returns>Base64 encoded fully signed transaction</returns>
    Task<string> AddSponsorSignatureAsync(string partiallySignedTransactionBase64);

    /// <summary>
    /// Verifies a transaction containing two USDC SPL token transfers (bird owner + Wihngo platform).
    /// Used for validating externally-submitted split payment transactions.
    /// </summary>
    /// <param name="signature">Transaction signature/hash to verify</param>
    /// <param name="expectedSender">Expected sender wallet public key</param>
    /// <param name="expectedBirdWallet">Expected bird owner's wallet public key</param>
    /// <param name="expectedBirdAmount">Expected USDC amount to bird owner (in USDC, not lamports)</param>
    /// <param name="expectedWihngoWallet">Expected Wihngo treasury wallet public key</param>
    /// <param name="expectedWihngoAmount">Expected USDC amount to Wihngo (in USDC, not lamports)</param>
    /// <returns>Verification result with details about each transfer</returns>
    Task<DualTransferVerificationResult> VerifyDualTransferTransactionAsync(
        string signature,
        string expectedSender,
        string expectedBirdWallet,
        decimal expectedBirdAmount,
        string expectedWihngoWallet,
        decimal expectedWihngoAmount);
}

/// <summary>
/// Result of transaction status check
/// </summary>
public class TransactionStatusResult
{
    public bool Found { get; set; }
    public bool Confirmed { get; set; }
    public bool Finalized { get; set; }
    public int Confirmations { get; set; }
    public long? Slot { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of transaction verification
/// </summary>
public class TransactionVerificationResult
{
    public bool Success { get; set; }
    public bool AmountMatches { get; set; }
    public bool SenderMatches { get; set; }
    public bool RecipientMatches { get; set; }
    public bool MintMatches { get; set; }
    public decimal ActualAmount { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of dual-transfer transaction verification (bird owner + Wihngo platform)
/// </summary>
public class DualTransferVerificationResult
{
    /// <summary>
    /// Whether the overall verification succeeded (both transfers valid)
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether the transaction was found and finalized on-chain
    /// </summary>
    public bool TransactionFound { get; set; }

    /// <summary>
    /// Whether the transaction executed without errors
    /// </summary>
    public bool TransactionSucceeded { get; set; }

    /// <summary>
    /// Whether the correct USDC mint was used
    /// </summary>
    public bool MintMatches { get; set; }

    /// <summary>
    /// Details about the bird owner transfer
    /// </summary>
    public TransferDetail? BirdTransfer { get; set; }

    /// <summary>
    /// Details about the Wihngo platform transfer
    /// </summary>
    public TransferDetail? WihngoTransfer { get; set; }

    /// <summary>
    /// The payer wallet extracted from the transaction
    /// </summary>
    public string? ActualPayer { get; set; }

    /// <summary>
    /// Whether the payer matches the expected sender
    /// </summary>
    public bool PayerMatches { get; set; }

    /// <summary>
    /// Number of USDC transfers found in the transaction
    /// </summary>
    public int UsdcTransferCount { get; set; }

    /// <summary>
    /// Error message if verification failed
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Details about a single transfer within a transaction
/// </summary>
public class TransferDetail
{
    /// <summary>
    /// Whether this transfer was found
    /// </summary>
    public bool Found { get; set; }

    /// <summary>
    /// Destination wallet public key
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Expected amount in USDC
    /// </summary>
    public decimal ExpectedAmount { get; set; }

    /// <summary>
    /// Actual amount transferred in USDC
    /// </summary>
    public decimal ActualAmount { get; set; }

    /// <summary>
    /// Whether the amount matches exactly
    /// </summary>
    public bool AmountMatches { get; set; }

    /// <summary>
    /// Whether the destination matches expected
    /// </summary>
    public bool DestinationMatches { get; set; }
}
