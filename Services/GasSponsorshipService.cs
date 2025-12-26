using Dapper;
using Microsoft.Extensions.Options;
using Wihngo.Configuration;
using Wihngo.Data;
using Wihngo.Models.Entities;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Service for managing gas sponsorship for P2P payments
/// </summary>
public class GasSponsorshipService : IGasSponsorshipService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ISolanaTransactionService _solanaService;
    private readonly P2PPaymentConfiguration _config;
    private readonly ILogger<GasSponsorshipService> _logger;

    public GasSponsorshipService(
        IDbConnectionFactory dbFactory,
        ISolanaTransactionService solanaService,
        IOptions<P2PPaymentConfiguration> config,
        ILogger<GasSponsorshipService> logger)
    {
        _dbFactory = dbFactory;
        _solanaService = solanaService;
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ShouldSponsorGasAsync(string senderPubkey)
    {
        if (!_config.GasSponsorship.Enabled)
        {
            return false;
        }

        try
        {
            var solBalance = await _solanaService.GetSolBalanceAsync(senderPubkey);
            var shouldSponsor = solBalance < _config.GasSponsorship.MinSolThreshold;

            _logger.LogDebug(
                "Gas sponsorship check for {Pubkey}: SOL balance = {Balance}, Threshold = {Threshold}, Sponsor = {Sponsor}",
                senderPubkey, solBalance, _config.GasSponsorship.MinSolThreshold, shouldSponsor);

            return shouldSponsor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if gas sponsorship needed for {Pubkey}", senderPubkey);
            // Default to sponsoring if we can't determine balance
            return true;
        }
    }

    /// <inheritdoc />
    public decimal GetSponsorshipFeeUsdc()
    {
        return _config.GasSponsorship.FlatFeeUsdc;
    }

    /// <inheritdoc />
    public decimal GetMinSolThreshold()
    {
        return _config.GasSponsorship.MinSolThreshold;
    }

    /// <inheritdoc />
    public Task<string?> GetSponsorWalletPubkeyAsync()
    {
        var pubkey = _config.GasSponsorship.SponsorWalletPubkey;
        return Task.FromResult(string.IsNullOrEmpty(pubkey) ? null : pubkey);
    }

    /// <inheritdoc />
    public async Task<PlatformHotWallet?> GetSponsorWalletAsync()
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        return await conn.QueryFirstOrDefaultAsync<PlatformHotWallet>(
            @"SELECT * FROM platform_hot_wallets
              WHERE wallet_type = @WalletType AND is_active = TRUE
              LIMIT 1",
            new { WalletType = PlatformWalletType.GasSponsor });
    }

    /// <inheritdoc />
    public async Task<GasSponsorship> RecordSponsorshipAsync(
        Guid paymentId,
        decimal sponsoredSolAmount,
        string sponsorWalletPubkey,
        bool ataCreated = false,
        string? ataAddress = null)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        var sponsorship = new GasSponsorship
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            SponsoredSolAmount = sponsoredSolAmount,
            FeeUsdcCharged = _config.GasSponsorship.FlatFeeUsdc,
            SponsorWalletPubkey = sponsorWalletPubkey,
            AtaCreated = ataCreated,
            AtaAddress = ataAddress,
            CreatedAt = DateTime.UtcNow
        };

        await conn.ExecuteAsync(
            @"INSERT INTO gas_sponsorships
              (id, payment_id, sponsored_sol_amount, fee_usdc_charged, sponsor_wallet_pubkey, ata_created, ata_address, created_at)
              VALUES (@Id, @PaymentId, @SponsoredSolAmount, @FeeUsdcCharged, @SponsorWalletPubkey, @AtaCreated, @AtaAddress, @CreatedAt)",
            sponsorship);

        _logger.LogInformation(
            "Recorded gas sponsorship for payment {PaymentId}: {SolAmount} SOL sponsored, {Fee} USDC fee, ATA created: {AtaCreated}",
            paymentId, sponsoredSolAmount, sponsorship.FeeUsdcCharged, ataCreated);

        return sponsorship;
    }

    /// <inheritdoc />
    public async Task<GasSponsorship?> GetSponsorshipAsync(Guid paymentId)
    {
        using var conn = await _dbFactory.CreateOpenConnectionAsync();

        return await conn.QueryFirstOrDefaultAsync<GasSponsorship>(
            "SELECT * FROM gas_sponsorships WHERE payment_id = @PaymentId",
            new { PaymentId = paymentId });
    }
}
