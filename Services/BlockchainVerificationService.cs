using System.Numerics;
using System.Text.Json;
using Nethereum.Web3;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

public class BlockchainVerificationService : IBlockchainService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlockchainVerificationService> _logger;

    public BlockchainVerificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<BlockchainVerificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TransactionInfo?> VerifyTransactionAsync(string txHash, string currency, string network)
    {
        try
        {
            return network.ToLower() switch
            {
                "tron" => await VerifyTronTransactionAsync(txHash, currency),
                "ethereum" or "polygon" or "binance-smart-chain" =>
                    await VerifyEvmTransactionAsync(txHash, currency, network),
                "bitcoin" => await VerifyBitcoinTransactionAsync(txHash),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error verifying transaction {txHash} on {network}");
            return null;
        }
    }

    private async Task<TransactionInfo?> VerifyTronTransactionAsync(string txHash, string currency)
    {
        try
        {
            var apiUrl = _configuration["BlockchainSettings:TronGrid:ApiUrl"] ?? "https://api.trongrid.io";
            var apiKey = _configuration["BlockchainSettings:TronGrid:ApiKey"];
            var client = _httpClientFactory.CreateClient();

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("TRON-PRO-API-KEY", apiKey);
            }

            // Get transaction
            var txResponse = await client.GetAsync($"{apiUrl}/wallet/gettransactionbyid?value={txHash}");
            if (!txResponse.IsSuccessStatusCode) return null;

            var txJson = await txResponse.Content.ReadAsStringAsync();
            var tx = JsonDocument.Parse(txJson);

            // Get transaction info
            var txInfoResponse = await client.GetAsync($"{apiUrl}/wallet/gettransactioninfobyid?value={txHash}");
            if (!txInfoResponse.IsSuccessStatusCode) return null;

            var txInfoJson = await txInfoResponse.Content.ReadAsStringAsync();
            var txInfo = JsonDocument.Parse(txInfoJson);

            // Check if successful
            if (txInfo.RootElement.TryGetProperty("receipt", out var receipt))
            {
                if (receipt.TryGetProperty("result", out var result) &&
                    result.GetString() != "SUCCESS")
                {
                    return null;
                }
            }

            decimal amount = 0;
            string toAddress = "";
            string fromAddress = "";

            if (currency == "USDT")
            {
                // TRC-20 USDT
                if (txInfo.RootElement.TryGetProperty("log", out var logs) && logs.GetArrayLength() > 0)
                {
                    var log = logs[0];
                    var data = log.GetProperty("data").GetString() ?? "";
                    var topics = log.GetProperty("topics").EnumerateArray().ToList();

                    // Decode amount (6 decimals)
                    amount = Convert.ToDecimal(BigInteger.Parse(data, System.Globalization.NumberStyles.HexNumber)) / 1_000_000m;

                    // Decode addresses
                    if (topics.Count >= 3)
                    {
                        toAddress = TronAddressFromHex("41" + topics[2].GetString()?.Substring(24));
                        fromAddress = TronAddressFromHex("41" + topics[1].GetString()?.Substring(24));
                    }
                }
            }

            // Get confirmations
            var currentBlockResponse = await client.GetAsync($"{apiUrl}/wallet/getnowblock");
            var currentBlockJson = await currentBlockResponse.Content.ReadAsStringAsync();
            var currentBlock = JsonDocument.Parse(currentBlockJson);

            var currentBlockNumber = currentBlock.RootElement
                .GetProperty("block_header")
                .GetProperty("raw_data")
                .GetProperty("number")
                .GetInt64();

            long? blockNumber = null;
            if (txInfo.RootElement.TryGetProperty("blockNumber", out var blockNumProp))
            {
                blockNumber = blockNumProp.GetInt64();
            }

            var confirmations = blockNumber.HasValue ? (int)(currentBlockNumber - blockNumber.Value + 1) : 0;

            return new TransactionInfo
            {
                Amount = amount,
                ToAddress = toAddress,
                FromAddress = fromAddress,
                Confirmations = confirmations,
                BlockNumber = blockNumber,
                BlockHash = txInfo.RootElement.TryGetProperty("blockHash", out var hash) ? hash.GetString() : null,
                Fee = txInfo.RootElement.TryGetProperty("fee", out var fee) ? fee.GetDecimal() / 1_000_000m : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"TRON verification error for {txHash}");
            return null;
        }
    }

    private async Task<TransactionInfo?> VerifyEvmTransactionAsync(string txHash, string currency, string network)
    {
        try
        {
            string rpcUrl = network.ToLower() switch
            {
                "ethereum" => $"https://mainnet.infura.io/v3/{_configuration["BlockchainSettings:Infura:ProjectId"]}",
                "binance-smart-chain" => "https://bsc-dataseed.binance.org",
                "polygon" => "https://polygon-rpc.com",
                _ => throw new NotSupportedException($"Network {network} not supported")
            };

            var web3 = new Web3(rpcUrl);
            var tx = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);

            if (tx == null || receipt == null) return null;

            // Check if transaction was successful
            if (receipt.Status?.Value != 1)
            {
                _logger.LogWarning($"EVM transaction {txHash} failed with status {receipt.Status?.Value}");
                return null;
            }

            var currentBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var confirmations = (int)(currentBlock.Value - receipt.BlockNumber.Value + 1);

            decimal amount = 0;
            string toAddress = tx.To ?? "";
            string fromAddress = tx.From ?? "";

            if (currency is "USDT" or "USDC")
            {
                // ERC-20 token - simplified verification
                // For production, use Nethereum's event decoding or manual parsing
                // For now, just check if transaction succeeded and assume amount is correct
                // The frontend will need to specify the amount, and we'll verify it matches
                
                // Log contains the actual transfer data but requires more complex parsing
                _logger.LogWarning($"ERC-20 token verification for {currency} is simplified. Manual amount verification recommended.");
                
                // Return basic info - amount will be verified against payment request
                amount = 0; // Will be validated against expected amount in service
            }
            else
            {
                // Native ETH/BNB
                amount = Web3.Convert.FromWei(tx.Value.Value);
            }

            var gasUsed = receipt.GasUsed?.Value ?? 0;
            var gasPrice = tx.GasPrice?.Value ?? 0;
            var fee = Web3.Convert.FromWei(gasUsed * gasPrice);

            return new TransactionInfo
            {
                Amount = amount,
                ToAddress = toAddress,
                FromAddress = fromAddress,
                Confirmations = confirmations,
                BlockNumber = (long)(receipt.BlockNumber?.Value ?? 0),
                BlockHash = receipt.BlockHash,
                Fee = fee
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"EVM verification error for {txHash}");
            return null;
        }
    }

    private async Task<TransactionInfo?> VerifyBitcoinTransactionAsync(string txHash)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://blockchain.info/rawtx/{txHash}");

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var tx = JsonDocument.Parse(json);

            var latestBlockResponse = await client.GetAsync("https://blockchain.info/latestblock");
            var latestBlockJson = await latestBlockResponse.Content.ReadAsStringAsync();
            var latestBlock = JsonDocument.Parse(latestBlockJson);
            var currentHeight = latestBlock.RootElement.GetProperty("height").GetInt64();

            long? blockHeight = null;
            if (tx.RootElement.TryGetProperty("block_height", out var blockProp))
            {
                blockHeight = blockProp.GetInt64();
            }

            var confirmations = blockHeight.HasValue ? (int)(currentHeight - blockHeight.Value + 1) : 0;

            var outputs = tx.RootElement.GetProperty("out").EnumerateArray().ToList();
            var inputs = tx.RootElement.GetProperty("inputs").EnumerateArray().ToList();

            return new TransactionInfo
            {
                Amount = outputs[0].GetProperty("value").GetDecimal() / 100_000_000m,
                ToAddress = outputs[0].GetProperty("addr").GetString() ?? "",
                FromAddress = inputs[0].GetProperty("prev_out").GetProperty("addr").GetString() ?? "",
                Confirmations = confirmations,
                BlockNumber = blockHeight,
                Fee = tx.RootElement.GetProperty("fee").GetDecimal() / 100_000_000m
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Bitcoin verification error for {txHash}");
            return null;
        }
    }

    private string TronAddressFromHex(string hex)
    {
        // Simplified TRON address conversion
        // In production, use proper TRON SDK
        return "T" + hex; // Placeholder
    }
}
