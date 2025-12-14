using System;
using Wihngo.Data;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

/// <summary>
/// Background service that continuously monitors blockchains for on-chain deposits
/// Runs at configured intervals to check for new deposits and update confirmation statuses
/// </summary>
public class OnChainDepositBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OnChainDepositBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _scanInterval;

    public OnChainDepositBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OnChainDepositBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        
        // Get scan interval from configuration (default: 30 seconds)
        var intervalSeconds = _configuration.GetValue<int>("OnChainDeposit:ScanIntervalSeconds", 30);
        _scanInterval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OnChainDepositBackgroundService started");

        // Wait a bit before starting to allow app initialization
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanForDepositsAsync(stoppingToken);
                await UpdatePendingDepositsAsync(stoppingToken);
                
                _logger.LogDebug("Completed scan cycle, waiting {Interval} before next scan", _scanInterval);
                await Task.Delay(_scanInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OnChainDepositBackgroundService stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnChainDepositBackgroundService scan cycle");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken); // Wait longer on error
            }
        }

        _logger.LogInformation("OnChainDepositBackgroundService stopped");
    }

    private async Task ScanForDepositsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var depositService = scope.ServiceProvider.GetRequiredService<IOnChainDepositService>();
        
        try
        {
            _logger.LogDebug("Scanning for new deposits...");

            // Get active token configurations
            var tokenConfigs = await depositService.GetActiveTokenConfigurationsAsync();
            
            if (tokenConfigs.Count == 0)
            {
                _logger.LogWarning("No active token configurations found");
                return;
            }

            // Get monitored addresses (implement based on your user address storage)
            // For now, this is a placeholder
            var monitoredAddresses = await GetMonitoredAddressesAsync(context);

            if (monitoredAddresses.Count == 0)
            {
                _logger.LogDebug("No monitored addresses configured yet");
                return;
            }

            // Scan each chain
            var evmChains = new[] { "ethereum", "polygon", "base" };
            
            foreach (var chain in evmChains)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                var chainConfigs = tokenConfigs.Where(t => t.Chain == chain).ToList();
                if (chainConfigs.Any())
                {
                    await ScanEvmChainAsync(scope, chain, chainConfigs, monitoredAddresses, cancellationToken);
                }
            }

            // Scan Solana
            var solanaConfigs = tokenConfigs.Where(t => t.Chain == "solana").ToList();
            if (solanaConfigs.Any())
            {
                await ScanSolanaAsync(scope, solanaConfigs, monitoredAddresses, cancellationToken);
            }

            // Scan Stellar
            var stellarConfigs = tokenConfigs.Where(t => t.Chain == "stellar").ToList();
            if (stellarConfigs.Any())
            {
                await ScanStellarAsync(scope, stellarConfigs, monitoredAddresses, cancellationToken);
            }

            _logger.LogDebug("Completed scanning for new deposits");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for deposits");
        }
    }

    private async Task ScanEvmChainAsync(
        IServiceScope scope,
        string chain,
        List<Models.Entities.TokenConfiguration> tokenConfigs,
        Dictionary<string, Guid> monitoredAddresses,
        CancellationToken cancellationToken)
    {
        try
        {
            var evmMonitor = scope.ServiceProvider.GetRequiredService<IEvmBlockchainMonitor>();
            
            _logger.LogDebug("Scanning {Chain} for deposits...", chain);

            // Get last scanned block from state (implement state storage)
            var lastBlock = await GetLastScannedBlockAsync(chain);
            var currentBlock = await evmMonitor.GetLatestBlockNumberAsync(chain);

            if (currentBlock > lastBlock)
            {
                var deposits = await evmMonitor.ScanBlocksForDepositsAsync(
                    chain, 
                    lastBlock + 1, 
                    currentBlock, 
                    monitoredAddresses.Keys.ToList());

                await ProcessNewDepositsAsync(scope, deposits, monitoredAddresses);
                await SaveLastScannedBlockAsync(chain, currentBlock);
                
                _logger.LogInformation(
                    "Scanned {Chain} blocks {From} to {To}, found {Count} deposits",
                    chain, lastBlock + 1, currentBlock, deposits.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning EVM chain {Chain}", chain);
        }
    }

    private async Task ScanSolanaAsync(
        IServiceScope scope,
        List<Models.Entities.TokenConfiguration> tokenConfigs,
        Dictionary<string, Guid> monitoredAddresses,
        CancellationToken cancellationToken)
    {
        try
        {
            var solanaMonitor = scope.ServiceProvider.GetRequiredService<ISolanaBlockchainMonitor>();
            
            _logger.LogDebug("Scanning Solana for deposits...");

            // TODO: Implement Solana scanning logic
            _logger.LogWarning("Solana scanning not fully implemented yet");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning Solana");
        }
    }

    private async Task ScanStellarAsync(
        IServiceScope scope,
        List<Models.Entities.TokenConfiguration> tokenConfigs,
        Dictionary<string, Guid> monitoredAddresses,
        CancellationToken cancellationToken)
    {
        try
        {
            var stellarMonitor = scope.ServiceProvider.GetRequiredService<IStellarBlockchainMonitor>();
            
            _logger.LogDebug("Scanning Stellar for deposits...");

            // TODO: Implement Stellar scanning logic
            _logger.LogWarning("Stellar scanning not fully implemented yet");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning Stellar");
        }
    }

    private async Task UpdatePendingDepositsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var depositService = scope.ServiceProvider.GetRequiredService<IOnChainDepositService>();
        
        try
        {
            _logger.LogDebug("Updating pending deposits...");

            var pendingDeposits = await depositService.GetPendingDepositsAsync();
            
            _logger.LogDebug("Found {Count} pending deposits to update", pendingDeposits.Count);

            foreach (var deposit in pendingDeposits)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    // Get token configuration
                    var tokenConfig = await depositService.GetTokenConfigurationAsync(deposit.Token, deposit.Chain);
                    if (tokenConfig == null) continue;

                    // Check confirmations based on chain type
                    var confirmed = false;
                    
                    if (deposit.Chain == "ethereum" || deposit.Chain == "polygon" || deposit.Chain == "base")
                    {
                        var evmMonitor = scope.ServiceProvider.GetRequiredService<IEvmBlockchainMonitor>();
                        var confirmations = await evmMonitor.GetTransactionConfirmationsAsync(deposit.TxHashOrSig, deposit.Chain);
                        
                        await depositService.UpdateDepositStatusAsync(deposit.Id, 
                            confirmations >= tokenConfig.RequiredConfirmations ? "confirmed" : "pending",
                            confirmations);
                        
                        confirmed = confirmations >= tokenConfig.RequiredConfirmations;
                    }
                    // Add similar logic for Solana and Stellar

                    // Credit user if confirmed
                    if (confirmed && deposit.Status != "credited")
                    {
                        await depositService.CreditDepositToUserAsync(deposit.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating deposit {DepositId}", deposit.Id);
                }
            }

            _logger.LogDebug("Completed updating pending deposits");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pending deposits");
        }
    }

    private async Task<Dictionary<string, Guid>> GetMonitoredAddressesAsync(AppDbContext context)
    {
        // TODO: Implement based on your user address storage
        // This should return a dictionary of address -> userId mappings
        return new Dictionary<string, Guid>();
    }

    private async Task ProcessNewDepositsAsync(
        IServiceScope scope, 
        List<Models.Entities.OnChainDeposit> deposits,
        Dictionary<string, Guid> addressToUserMap)
    {
        var depositService = scope.ServiceProvider.GetRequiredService<IOnChainDepositService>();

        foreach (var deposit in deposits)
        {
            try
            {
                // Map address to user ID
                if (addressToUserMap.TryGetValue(deposit.AddressOrAccount, out var userId))
                {
                    deposit.UserId = userId;
                    await depositService.RecordDepositAsync(deposit);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing deposit for tx {TxHash}", deposit.TxHashOrSig);
            }
        }
    }

    private async Task<long> GetLastScannedBlockAsync(string chain)
    {
        // TODO: Implement state storage (could use database, Redis, or file)
        // For now, return a default
        return 0;
    }

    private async Task SaveLastScannedBlockAsync(string chain, long blockNumber)
    {
        // TODO: Implement state storage
        await Task.CompletedTask;
    }
}
