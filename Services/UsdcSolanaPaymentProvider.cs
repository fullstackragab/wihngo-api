using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Models.Enums;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// USDC on Solana payment provider.
/// Validates transactions via Solana RPC.
///
/// Responsibilities:
/// - Validate USDC mint address (configurable for devnet/mainnet)
/// - Validate destination wallet
/// - Validate amount (exact match)
/// - Validate memo contains payment intent ID (replay protection)
/// - Confirm transaction is finalized
///
/// Returns verification result only, never side effects.
/// </summary>
public sealed class UsdcSolanaPaymentProvider : IPaymentProvider
{
    // USDC has 6 decimals
    private const int UsdcDecimals = 6;

    // Memo prefix for payment binding (prevents replay attacks)
    private const string MemoPrefix = "wihngo:";

    private readonly HttpClient _httpClient;
    private readonly SolanaConfiguration _settings;
    private readonly ILogger<UsdcSolanaPaymentProvider> _logger;

    public PaymentProvider ProviderType => PaymentProvider.UsdcSolana;

    public UsdcSolanaPaymentProvider(
        HttpClient httpClient,
        IOptions<SolanaConfiguration> settings,
        ILogger<UsdcSolanaPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<PaymentIntentResult> CreateIntentAsync(
        CreatePaymentIntentCommand command,
        CancellationToken ct = default)
    {
        // For USDC, the "intent" is simply returning the destination wallet
        // and expected amount. The user sends USDC via Phantom, then submits the tx hash.

        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.PaymentIntentExpiryMinutes);

        var result = new PaymentIntentResult(
            PaymentId: Guid.Empty, // Will be set by PaymentService
            AmountCents: command.AmountCents,
            DestinationWallet: _settings.PlatformWalletAddress,
            TokenMint: _settings.UsdcMintAddress,
            ExpiresAt: expiresAt
        );

        _logger.LogDebug(
            "Created USDC payment intent for {Amount} cents to wallet {Wallet} on {Network}",
            command.AmountCents, _settings.PlatformWalletAddress, _settings.RpcUrl);

        return Task.FromResult(result);
    }

    public async Task<PaymentVerificationResult> VerifyAsync(
        VerifyPaymentCommand command,
        CancellationToken ct = default)
    {
        var txHash = command.ProviderRef;

        _logger.LogDebug("Verifying Solana transaction {TxHash}", txHash);

        try
        {
            // Fetch transaction from Solana RPC with retries
            // Transactions may take 2-15 seconds to finalize on Solana
            var txInfo = await GetTransactionWithRetryAsync(txHash, ct);

            if (txInfo is null)
            {
                _logger.LogWarning("Transaction {TxHash} not found on Solana after retries", txHash);
                return PaymentVerificationResult.Invalid("Transaction not found or not yet finalized. Please try again in a few seconds.");
            }

            // Check if transaction is finalized
            if (txInfo.Meta?.Err is not null)
            {
                _logger.LogWarning("Transaction {TxHash} failed on-chain", txHash);
                return PaymentVerificationResult.Invalid("Transaction failed on-chain");
            }

            // Parse token transfer from transaction
            var transfer = ParseTokenTransfer(txInfo);

            if (transfer is null)
            {
                _logger.LogWarning("Transaction {TxHash} has no valid USDC transfer", txHash);
                return PaymentVerificationResult.Invalid("No valid USDC transfer found");
            }

            // Validate token mint is USDC
            if (transfer.TokenMint != _settings.UsdcMintAddress)
            {
                _logger.LogWarning(
                    "Transaction {TxHash} uses wrong token mint: {Mint}, expected: {Expected}",
                    txHash, transfer.TokenMint, _settings.UsdcMintAddress);
                return PaymentVerificationResult.Invalid("Invalid token: not USDC");
            }

            // Validate destination wallet owner (not the token account, but the wallet that owns it)
            if (string.IsNullOrEmpty(transfer.DestinationOwner))
            {
                _logger.LogWarning(
                    "Transaction {TxHash} could not determine destination wallet owner",
                    txHash);
                return PaymentVerificationResult.Invalid("Could not verify destination wallet");
            }

            if (transfer.DestinationOwner != _settings.PlatformWalletAddress)
            {
                _logger.LogWarning(
                    "Transaction {TxHash} sent to wrong wallet: {Wallet}, expected: {Expected}",
                    txHash, transfer.DestinationOwner, _settings.PlatformWalletAddress);
                return PaymentVerificationResult.Invalid("Invalid destination wallet");
            }

            // Validate memo contains payment intent ID (replay protection)
            var memo = ParseMemo(txInfo);
            var expectedMemo = $"{MemoPrefix}{command.PaymentId}";

            if (string.IsNullOrEmpty(memo))
            {
                _logger.LogWarning(
                    "Transaction {TxHash} missing memo instruction",
                    txHash);
                return PaymentVerificationResult.Invalid("Missing payment reference memo");
            }

            if (memo != expectedMemo)
            {
                _logger.LogWarning(
                    "Transaction {TxHash} memo mismatch: {Memo}, expected: {Expected}",
                    txHash, memo, expectedMemo);
                return PaymentVerificationResult.Invalid("Invalid payment reference");
            }

            // Convert USDC amount (6 decimals) to cents
            var amountCents = ConvertUsdcToCents(transfer.Amount);

            // Get block time
            var blockTime = txInfo.BlockTime.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(txInfo.BlockTime.Value).UtcDateTime
                : DateTime.UtcNow;

            _logger.LogInformation(
                "Verified Solana transaction {TxHash}: {Amount} cents from {Sender}",
                txHash, amountCents, transfer.Source);

            return PaymentVerificationResult.Success(
                senderWallet: transfer.Source,
                txHash: txHash,
                blockTime: blockTime,
                verifiedAmountCents: amountCents
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Solana transaction {TxHash}", txHash);
            return PaymentVerificationResult.Invalid($"Verification error: {ex.Message}");
        }
    }

    private async Task<SolanaTransactionInfo?> GetTransactionWithRetryAsync(string txHash, CancellationToken ct)
    {
        // Retry up to 5 times with increasing delays (total ~15 seconds)
        // Solana finalization takes ~13 seconds (31 confirmations at ~400ms each)
        int[] delaysMs = [0, 2000, 3000, 4000, 5000];

        for (int attempt = 0; attempt < delaysMs.Length; attempt++)
        {
            if (attempt > 0)
            {
                _logger.LogDebug(
                    "Transaction {TxHash} not found, retrying in {Delay}ms (attempt {Attempt}/{Max})",
                    txHash, delaysMs[attempt], attempt + 1, delaysMs.Length);

                await Task.Delay(delaysMs[attempt], ct);
            }

            var txInfo = await GetTransactionAsync(txHash, ct);
            if (txInfo is not null)
            {
                _logger.LogDebug(
                    "Transaction {TxHash} found on attempt {Attempt}",
                    txHash, attempt + 1);
                return txInfo;
            }
        }

        return null;
    }

    private async Task<SolanaTransactionInfo?> GetTransactionAsync(string txHash, CancellationToken ct)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "getTransaction",
            @params = new object[]
            {
                txHash,
                new { encoding = "jsonParsed", commitment = "finalized", maxSupportedTransactionVersion = 0 }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(_settings.RpcUrl, request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SolanaRpcResponse<SolanaTransactionInfo>>(ct);
        return result?.Result;
    }

    private TokenTransferInfo? ParseTokenTransfer(SolanaTransactionInfo txInfo)
    {
        // Look for SPL token transfer instruction in the parsed transaction
        var instructions = txInfo.Transaction?.Message?.Instructions;
        if (instructions is null)
            return null;

        foreach (var instruction in instructions)
        {
            // Check for SPL Token transfer or transferChecked
            if (instruction.Program != "spl-token")
                continue;

            // Use helper method to safely parse the token instruction
            var parsed = instruction.GetParsedTokenInstruction();
            if (parsed is null)
                continue;

            var type = parsed.Type;
            if (type != "transfer" && type != "transferChecked")
                continue;

            var info = parsed.Info;
            if (info is null)
                continue;

            // For transferChecked, we also get the mint
            var tokenMint = info.Mint ?? TryGetMintFromPostBalances(txInfo, info.Destination);

            if (tokenMint is null)
                continue;

            // Get the owner of the destination token account from postTokenBalances
            // The destination in SPL transfers is the token account (ATA), not the wallet
            var destinationOwner = TryGetOwnerFromPostBalances(txInfo, info.Destination);

            return new TokenTransferInfo(
                Source: info.Source ?? info.Authority ?? string.Empty,
                Destination: info.Destination ?? string.Empty,
                DestinationOwner: destinationOwner ?? string.Empty,
                Amount: info.TokenAmount?.Amount ?? info.Amount ?? "0",
                TokenMint: tokenMint
            );
        }

        return null;
    }

    private string? TryGetMintFromPostBalances(SolanaTransactionInfo txInfo, string? tokenAccount)
    {
        if (tokenAccount is null)
            return null;

        // Try to find mint from post token balances
        var postTokenBalances = txInfo.Meta?.PostTokenBalances;
        if (postTokenBalances is null)
            return null;

        foreach (var balance in postTokenBalances)
        {
            // Match by account index in account keys
            var accountKeys = txInfo.Transaction?.Message?.AccountKeys;
            if (accountKeys is not null && balance.AccountIndex < accountKeys.Count)
            {
                var account = accountKeys[balance.AccountIndex];
                if (account.Pubkey == tokenAccount)
                    return balance.Mint;
            }
        }

        return null;
    }

    private string? TryGetOwnerFromPostBalances(SolanaTransactionInfo txInfo, string? tokenAccount)
    {
        if (tokenAccount is null)
            return null;

        // Get the owner wallet address from post token balances
        var postTokenBalances = txInfo.Meta?.PostTokenBalances;
        if (postTokenBalances is null)
            return null;

        foreach (var balance in postTokenBalances)
        {
            var accountKeys = txInfo.Transaction?.Message?.AccountKeys;
            if (accountKeys is not null && balance.AccountIndex < accountKeys.Count)
            {
                var account = accountKeys[balance.AccountIndex];
                if (account.Pubkey == tokenAccount)
                    return balance.Owner;
            }
        }

        return null;
    }

    private static string? ParseMemo(SolanaTransactionInfo txInfo)
    {
        // Look for memo instruction in the transaction
        // Memo program ID: MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr
        var instructions = txInfo.Transaction?.Message?.Instructions;
        if (instructions is null)
            return null;

        foreach (var instruction in instructions)
        {
            // Check for memo program (appears as "spl-memo" in jsonParsed)
            if (instruction.Program == "spl-memo" && instruction.Parsed is not null)
            {
                // For parsed memo, the content is directly in Parsed as a string
                var memoContent = instruction.GetParsedString();
                if (memoContent is not null)
                    return memoContent;

                // If not a string, try to get the raw JSON representation
                return instruction.Parsed.Value.ToString();
            }

            // Also check programId for raw memo instructions
            if (instruction.ProgramId == "MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr" ||
                instruction.ProgramId == "Memo1UhkJRfHyvLMcVucJwxXeuD728EqVDDwQDxFMNo")
            {
                // Raw memo data is base64 encoded in the data field
                if (!string.IsNullOrEmpty(instruction.Data))
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(instruction.Data);
                        return System.Text.Encoding.UTF8.GetString(bytes);
                    }
                    catch
                    {
                        // If not base64, try direct string
                        return instruction.Data;
                    }
                }
            }
        }

        return null;
    }

    private static int ConvertUsdcToCents(string usdcAmount)
    {
        // USDC has 6 decimals, so 1 USDC = 1_000_000 base units
        // 1 cent = 0.01 USD = 10_000 base units
        if (!long.TryParse(usdcAmount, out var baseUnits))
            return 0;

        // Convert to cents: baseUnits / 10_000
        return (int)(baseUnits / 10_000);
    }

    private sealed record TokenTransferInfo(
        string Source,
        string Destination,
        string DestinationOwner,
        string Amount,
        string TokenMint
    );
}

// Solana RPC response types

internal sealed class SolanaRpcResponse<T>
{
    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("error")]
    public SolanaRpcError? Error { get; set; }
}

internal sealed class SolanaRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

internal sealed class SolanaTransactionInfo
{
    [JsonPropertyName("blockTime")]
    public long? BlockTime { get; set; }

    [JsonPropertyName("meta")]
    public SolanaTransactionMeta? Meta { get; set; }

    [JsonPropertyName("transaction")]
    public SolanaTransaction? Transaction { get; set; }
}

internal sealed class SolanaTransactionMeta
{
    [JsonPropertyName("err")]
    public object? Err { get; set; }

    [JsonPropertyName("postTokenBalances")]
    public List<SolanaTokenBalance>? PostTokenBalances { get; set; }
}

internal sealed class SolanaTokenBalance
{
    [JsonPropertyName("accountIndex")]
    public int AccountIndex { get; set; }

    [JsonPropertyName("mint")]
    public string? Mint { get; set; }

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }
}

internal sealed class SolanaTransaction
{
    [JsonPropertyName("message")]
    public SolanaMessage? Message { get; set; }
}

internal sealed class SolanaMessage
{
    [JsonPropertyName("accountKeys")]
    public List<SolanaAccountKey>? AccountKeys { get; set; }

    [JsonPropertyName("instructions")]
    public List<SolanaInstruction>? Instructions { get; set; }
}

internal sealed class SolanaAccountKey
{
    [JsonPropertyName("pubkey")]
    public string? Pubkey { get; set; }
}

internal sealed class SolanaInstruction
{
    [JsonPropertyName("program")]
    public string? Program { get; set; }

    [JsonPropertyName("programId")]
    public string? ProgramId { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>
    /// Parsed instruction data. Can be an object (for token transfers) or a string (for memos).
    /// Use JsonElement to handle varying structures from different Solana programs.
    /// </summary>
    [JsonPropertyName("parsed")]
    public JsonElement? Parsed { get; set; }

    /// <summary>
    /// Try to get parsed instruction as a token transfer instruction.
    /// Returns null if not a valid token instruction structure.
    /// </summary>
    public SolanaParsedTokenInstruction? GetParsedTokenInstruction()
    {
        if (Parsed is null || Parsed.Value.ValueKind != JsonValueKind.Object)
            return null;

        try
        {
            return JsonSerializer.Deserialize<SolanaParsedTokenInstruction>(Parsed.Value.GetRawText());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Try to get parsed instruction as a string (e.g., memo content).
    /// </summary>
    public string? GetParsedString()
    {
        if (Parsed is null)
            return null;

        if (Parsed.Value.ValueKind == JsonValueKind.String)
            return Parsed.Value.GetString();

        return null;
    }
}

internal sealed class SolanaParsedTokenInstruction
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("info")]
    public SolanaTransferInfo? Info { get; set; }
}

internal sealed class SolanaTransferInfo
{
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("destination")]
    public string? Destination { get; set; }

    [JsonPropertyName("authority")]
    public string? Authority { get; set; }

    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("mint")]
    public string? Mint { get; set; }

    [JsonPropertyName("tokenAmount")]
    public SolanaTokenAmount? TokenAmount { get; set; }
}

internal sealed class SolanaTokenAmount
{
    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("decimals")]
    public int Decimals { get; set; }
}
