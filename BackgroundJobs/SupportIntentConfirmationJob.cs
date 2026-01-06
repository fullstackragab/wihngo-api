using Dapper;
using Wihngo.Data;
using Wihngo.Services.Interfaces;

namespace Wihngo.BackgroundJobs;

/// <summary>
/// Background job that polls Solana for support intent transaction confirmations.
/// Checks support_intents with status 'processing' and confirms them when finalized on-chain.
/// </summary>
public class SupportIntentConfirmationJob
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ISolanaTransactionService _solanaService;
    private readonly ISupportIntentService _supportIntentService;
    private readonly ILogger<SupportIntentConfirmationJob> _logger;

    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(30);

    public SupportIntentConfirmationJob(
        IDbConnectionFactory dbFactory,
        ISolanaTransactionService solanaService,
        ISupportIntentService supportIntentService,
        ILogger<SupportIntentConfirmationJob> logger)
    {
        _dbFactory = dbFactory;
        _solanaService = solanaService;
        _supportIntentService = supportIntentService;
        _logger = logger;
    }

    /// <summary>
    /// Checks all processing support intents for confirmation.
    /// Should be run every 10 seconds via Hangfire.
    /// </summary>
    public async Task CheckPendingIntentsAsync()
    {
        try
        {
            using var conn = await _dbFactory.CreateOpenConnectionAsync();

            // Get all support intents that are processing and have a signature
            var pendingIntents = await conn.QueryAsync<PendingIntent>(@"
                SELECT id, solana_signature, paid_at
                FROM support_intents
                WHERE status = 'processing'
                  AND solana_signature IS NOT NULL
                  AND solana_signature != ''
                ORDER BY paid_at ASC
                LIMIT 50");

            var intents = pendingIntents.ToList();

            if (intents.Count == 0)
            {
                return;
            }

            _logger.LogDebug("Checking {Count} processing support intents for confirmation", intents.Count);

            foreach (var intent in intents)
            {
                try
                {
                    // Check for timeout
                    if (intent.PaidAt.HasValue &&
                        DateTime.UtcNow - intent.PaidAt.Value > ProcessingTimeout)
                    {
                        _logger.LogWarning(
                            "Support intent {IntentId} timed out after {Minutes} minutes",
                            intent.Id,
                            (DateTime.UtcNow - intent.PaidAt.Value).TotalMinutes);

                        // Mark as failed due to timeout
                        await conn.ExecuteAsync(
                            "UPDATE support_intents SET status = 'failed', updated_at = @Now WHERE id = @IntentId",
                            new { Now = DateTime.UtcNow, IntentId = intent.Id });
                        continue;
                    }

                    // Check transaction status on Solana
                    var status = await _solanaService.GetTransactionStatusAsync(intent.SolanaSignature);

                    if (!status.Found)
                    {
                        _logger.LogDebug("Transaction {Signature} not yet found for intent {IntentId}",
                            intent.SolanaSignature, intent.Id);
                        continue;
                    }

                    if (status.Error != null)
                    {
                        // Transaction failed on-chain
                        _logger.LogError("Support intent {IntentId} failed on-chain: {Error}",
                            intent.Id, status.Error);

                        await conn.ExecuteAsync(
                            "UPDATE support_intents SET status = 'failed', updated_at = @Now WHERE id = @IntentId",
                            new { Now = DateTime.UtcNow, IntentId = intent.Id });
                        continue;
                    }

                    // Transaction confirmed - check if finalized or confirmed
                    if (status.Finalized || status.Confirmed)
                    {
                        var confirmations = status.Confirmations > 0 ? status.Confirmations : 1;
                        var confirmed = await _supportIntentService.ConfirmTransactionAsync(intent.Id, confirmations);

                        if (confirmed)
                        {
                            _logger.LogInformation(
                                "Support intent {IntentId} confirmed with {Confirmations} confirmations (finalized: {Finalized})",
                                intent.Id, confirmations, status.Finalized);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking confirmation for support intent {IntentId}", intent.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SupportIntentConfirmationJob");
        }
    }

    private class PendingIntent
    {
        public Guid Id { get; set; }
        public string SolanaSignature { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }
}
