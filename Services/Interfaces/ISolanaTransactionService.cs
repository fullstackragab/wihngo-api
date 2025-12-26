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
    /// <param name="recipientPubkey">Recipient's wallet public key</param>
    /// <param name="amountUsdc">Amount of USDC to transfer</param>
    /// <param name="feePayer">Optional fee payer (platform wallet if sponsoring)</param>
    /// <param name="createRecipientAta">Whether to include ATA creation for recipient</param>
    /// <returns>Base64 encoded unsigned transaction</returns>
    Task<string> BuildUsdcTransferTransactionAsync(
        string senderPubkey,
        string recipientPubkey,
        decimal amountUsdc,
        string? feePayer = null,
        bool createRecipientAta = false);

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
