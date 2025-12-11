using System.Numerics;
using System.Text.Json;
using Nethereum.Web3;
using Nethereum.Hex.HexTypes;
using Nethereum.ABI.FunctionEncoding;
using Wihngo.Services.Interfaces;

namespace Wihngo.Services;

public class BlockchainVerificationService : IBlockchainService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlockchainVerificationService> _logger;

    // ERC-20 Transfer event signature: Transfer(address,address,uint256)
    private const string ERC20_TRANSFER_EVENT_SIGNATURE = "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef";

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
                "ethereum" or "polygon" or "binance-smart-chain" or "sepolia" =>
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

                    // Decode amount (6 decimals for USDT)
                    if (!string.IsNullOrEmpty(data))
                    {
                        // Use explicit cast from BigInteger to decimal to avoid InvalidCastException
                        var big = BigInteger.Parse(data, System.Globalization.NumberStyles.HexNumber);
                        amount = (decimal)big / 1_000_000m;
                    }

                    // Decode addresses from topics
                    if (topics.Count >= 3)
                    {
                        var toHex = topics[2].GetString();
                        var fromHex = topics[1].GetString();
                        
                        if (!string.IsNullOrEmpty(toHex) && toHex.Length >= 24)
                        {
                            toAddress = TronAddressConverter.HexToBase58("41" + toHex.Substring(24));
                        }
                        
                        if (!string.IsNullOrEmpty(fromHex) && fromHex.Length >= 24)
                        {
                            fromAddress = TronAddressConverter.HexToBase58("41" + fromHex.Substring(24));
                        }
                    }
                }
            }
            else if (currency == "TRX")
            {
                // Native TRX transfer
                if (tx.RootElement.TryGetProperty("raw_data", out var rawData))
                {
                    if (rawData.TryGetProperty("contract", out var contracts) && contracts.GetArrayLength() > 0)
                    {
                        var contract = contracts[0];
                        if (contract.TryGetProperty("parameter", out var parameter))
                        {
                            if (parameter.TryGetProperty("value", out var value))
                            {
                                if (value.TryGetProperty("amount", out var amountProp))
                                {
                                    amount = amountProp.GetDecimal() / 1_000_000m; // TRX has 6 decimals
                                }
                                if (value.TryGetProperty("to_address", out var toAddressProp))
                                {
                                    var toHex = toAddressProp.GetString();
                                    if (!string.IsNullOrEmpty(toHex))
                                    {
                                        toAddress = TronAddressConverter.HexToBase58(toHex);
                                    }
                                }
                                if (value.TryGetProperty("owner_address", out var ownerAddressProp))
                                {
                                    var fromHex = ownerAddressProp.GetString();
                                    if (!string.IsNullOrEmpty(fromHex))
                                    {
                                        fromAddress = TronAddressConverter.HexToBase58(fromHex);
                                    }
                                }
                            }
                        }
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
            Console.WriteLine($"[BlockchainVerify] Verifying EVM transaction:");
            Console.WriteLine($"   TxHash: {txHash}");
            Console.WriteLine($"   Currency: {currency}");
            Console.WriteLine($"   Network: {network}");

            string rpcUrl = network.ToLower() switch
            {
                "ethereum" => $"https://mainnet.infura.io/v3/{_configuration["BlockchainSettings:Infura:ProjectId"]}",
                "sepolia" => $"https://sepolia.infura.io/v3/{_configuration["BlockchainSettings:Infura:ProjectId"]}",
                "binance-smart-chain" => "https://bsc-dataseed.binance.org",
                "polygon" => "https://polygon-rpc.com",
                _ => throw new NotSupportedException($"Network {network} not supported")
            };

            Console.WriteLine($"   RPC URL: {rpcUrl.Replace(_configuration["BlockchainSettings:Infura:ProjectId"] ?? "", "***")}");

            var web3 = new Web3(rpcUrl);
            
            Console.WriteLine($"   Fetching transaction...");
            var tx = await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
            
            Console.WriteLine($"   Fetching receipt...");
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);

            if (tx == null || receipt == null)
            {
                Console.WriteLine($"   ? Transaction or receipt is NULL");
                Console.WriteLine($"   - Transaction: {(tx == null ? "NULL" : "OK")}");
                Console.WriteLine($"   - Receipt: {(receipt == null ? "NULL" : "OK")}");
                return null;
            }

            Console.WriteLine($"   ? Transaction found!");
            Console.WriteLine($"   - From: {tx.From}");
            Console.WriteLine($"   - To: {tx.To}");
            Console.WriteLine($"   - Block: {receipt.BlockNumber?.Value}");
            Console.WriteLine($"   - Status: {receipt.Status?.Value}");

            // Check if transaction was successful
            if (receipt.Status?.Value != 1)
            {
                Console.WriteLine($"   ? Transaction FAILED (status != 1)");
                _logger.LogWarning($"EVM transaction {txHash} failed with status {receipt.Status?.Value}");
                return null;
            }

            var currentBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var confirmations = (int)(currentBlock.Value - receipt.BlockNumber.Value + 1);
            
            Console.WriteLine($"   - Current block: {currentBlock.Value}");
            Console.WriteLine($"   - Confirmations: {confirmations}");

            decimal amount = 0;
            string toAddress = tx.To ?? "";
            string fromAddress = tx.From ?? "";

            if (currency is "USDT" or "USDC")
            {
                Console.WriteLine($"   Processing ERC-20 token: {currency}");
                
                if (receipt.Logs != null && receipt.Logs.Count > 0)
                {
                    Console.WriteLine($"   - Found {receipt.Logs.Count} log entries");
                    
                    // Find Transfer event in logs
                    int logIndex = 0;
                    foreach (var log in receipt.Logs)
                    {
                        var topics = log["topics"];
                        if (topics != null && topics.HasValues)
                        {
                            var topicsArray = topics.ToObject<string[]>();
                            if (topicsArray != null && topicsArray.Length > 0)
                            {
                                var eventSignature = topicsArray[0];
                                
                                Console.WriteLine($"   - Log {logIndex}: Event signature = {eventSignature}");
                                
                                if (eventSignature.Equals(ERC20_TRANSFER_EVENT_SIGNATURE, StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine($"   ? Found Transfer event!");
                                    
                                    if (topicsArray.Length >= 3)
                                    {
                                        var fromHex = topicsArray[1];
                                        if (fromHex.StartsWith("0x") && fromHex.Length >= 66)
                                        {
                                            fromAddress = "0x" + fromHex.Substring(26);
                                            Console.WriteLine($"   - From address: {fromAddress}");
                                        }
                                        
                                        var toHex = topicsArray[2];
                                        if (toHex.StartsWith("0x") && toHex.Length >= 66)
                                        {
                                            toAddress = "0x" + toHex.Substring(26);
                                            Console.WriteLine($"   - To address: {toAddress}");
                                        }
                                    }
                                    
                                    var dataToken = log["data"];
                                    if (dataToken != null)
                                    {
                                        var dataHex = dataToken.ToString();
                                        if (!string.IsNullOrEmpty(dataHex) && dataHex.StartsWith("0x"))
                                        {
                                            var hexValue = dataHex.Substring(2);
                                            
                                            if (hexValue.Length > 0)
                                            {
                                                var amountWei = BigInteger.Parse("0" + hexValue, System.Globalization.NumberStyles.HexNumber);
                                                var decimals = GetTokenDecimals(currency, network);
                                                var divisor = (decimal)BigInteger.Pow(10, decimals);
                                                amount = (decimal)amountWei / divisor;
                                                
                                                Console.WriteLine($"   - Raw amount: {amountWei}");
                                                Console.WriteLine($"   - Decimals: {decimals}");
                                                Console.WriteLine($"   - Final amount: {amount} {currency}");
                                            }
                                        }
                                    }
                                    
                                    break;
                                }
                            }
                        }
                        logIndex++;
                    }
                    
                    if (amount == 0)
                    {
                        Console.WriteLine($"   ? WARNING: Could not decode amount from Transfer event");
                        _logger.LogWarning($"Could not decode amount from ERC-20 Transfer event for {txHash}");
                    }
                }
                else
                {
                    Console.WriteLine($"   ? WARNING: No logs found in receipt");
                    _logger.LogWarning($"No logs found in receipt for ERC-20 transaction {txHash}");
                }
            }
            else
            {
                // Native ETH/BNB/Sepolia ETH
                amount = Web3.Convert.FromWei(tx.Value.Value);
                Console.WriteLine($"   Native token amount: {amount} {currency}");
            }

            var gasUsed = receipt.GasUsed?.Value ?? 0;
            var gasPrice = tx.GasPrice?.Value ?? 0;
            var fee = Web3.Convert.FromWei(gasUsed * gasPrice);

            Console.WriteLine($"   ? Verification complete!");
            Console.WriteLine($"   - Amount: {amount}");
            Console.WriteLine($"   - From: {fromAddress}");
            Console.WriteLine($"   - To: {toAddress}");
            Console.WriteLine($"   - Confirmations: {confirmations}");

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
            Console.WriteLine($"   ? ERROR: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            _logger.LogError(ex, $"EVM verification error for {txHash}");
            return null;
        }
    }

    /// <summary>
    /// Get the number of decimals for a specific token on a specific network
    /// </summary>
    private int GetTokenDecimals(string currency, string network)
    {
        var key = $"{currency.ToUpper()}_{network.ToLower()}";
        
        return key switch
        {
            // USDT implementations
            "USDT_tron" => 6,                    // TRC-20 USDT
            "USDT_ethereum" => 6,                // ERC-20 USDT
            "USDT_sepolia" => 6,                 // Sepolia Testnet USDT
            "USDT_binance-smart-chain" => 18,   // BEP-20 USDT
            "USDT_polygon" => 6,                 // Polygon USDT
            
            // USDC implementations
            "USDC_ethereum" => 6,                // ERC-20 USDC
            "USDC_sepolia" => 6,                 // Sepolia Testnet USDC
            "USDC_binance-smart-chain" => 18,   // BEP-20 USDC
            "USDC_polygon" => 6,                 // Polygon USDC
            
            // ETH implementations
            "ETH_ethereum" => 18,                // Mainnet ETH
            "ETH_sepolia" => 18,                 // Sepolia Testnet ETH
            
            // Default to 6 decimals (most common for stablecoins)
            _ => 6
        };
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
}
