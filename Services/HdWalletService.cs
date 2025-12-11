using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Nethereum.Web3.Accounts;
using Wihngo.Services.Interfaces;
using NBitcoinKeyPath = NBitcoin.KeyPath;

namespace Wihngo.Services
{
    public class HdWalletService : IHdWalletService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HdWalletService> _logger;

        public HdWalletService(IConfiguration configuration, ILogger<HdWalletService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Derives an address using BIP44 path for the given index and network.
        /// Standard BIP44 path: m/44'/coin_type'/account'/change/address_index
        /// For Ethereum-compatible chains: m/44'/60'/0'/0/{index}
        /// For Bitcoin: m/44'/0'/0'/0/{index}
        /// For Tron: Uses Ethereum derivation then converts to Tron format
        /// </summary>
        /// <param name="mnemonicOrXprv">BIP39 mnemonic phrase or extended private key (xprv)</param>
        /// <param name="network">Network name (ethereum, sepolia, tron, bitcoin, etc.)</param>
        /// <param name="index">Address index in the derivation path (starting from 0)</param>
        /// <returns>Derived address or null on error</returns>
        public string? DeriveAddress(string mnemonicOrXprv, string network, int index = 0)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mnemonicOrXprv))
                {
                    _logger.LogError("HD derivation failed: mnemonic/xprv is null or empty");
                    return null;
                }

                if (index < 0)
                {
                    _logger.LogError("HD derivation failed: index cannot be negative ({Index})", index);
                    return null;
                }

                var normalizedNetwork = network?.ToLower() ?? "ethereum";
                
                _logger.LogInformation("Deriving HD address for network={Network}, index={Index}", normalizedNetwork, index);

                // Determine BIP44 coin type based on network
                string derivationPath = GetDerivationPath(normalizedNetwork, index);
                
                _logger.LogDebug("Using derivation path: {Path}", derivationPath);

                // Check if input is a mnemonic (contains spaces and at least 12 words)
                if (mnemonicOrXprv.Trim().Contains(' '))
                {
                    var words = mnemonicOrXprv.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    
                    if (words.Length < 12 || words.Length > 24)
                    {
                        _logger.LogError("Invalid mnemonic: expected 12-24 words, got {Count}", words.Length);
                        return null;
                    }

                    if (words.Length % 3 != 0)
                    {
                        _logger.LogWarning("Mnemonic word count ({Count}) is not standard (12, 15, 18, 21, or 24)", words.Length);
                    }

                    var mnemonic = new Mnemonic(mnemonicOrXprv.Trim());
                    var masterKey = mnemonic.DeriveExtKey();
                    
                    return DeriveFromMasterKey(masterKey, derivationPath, normalizedNetwork, index);
                }
                // Check if input is an extended private key
                else if (mnemonicOrXprv.StartsWith("xprv") || mnemonicOrXprv.StartsWith("tprv"))
                {
                    var network_type = mnemonicOrXprv.StartsWith("tprv") ? Network.TestNet : Network.Main;
                    var masterKey = ExtKey.Parse(mnemonicOrXprv, network_type);
                    
                    return DeriveFromMasterKey(masterKey, derivationPath, normalizedNetwork, index);
                }
                else
                {
                    _logger.LogError("Invalid input: must be either a BIP39 mnemonic or extended private key (xprv/tprv)");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to derive HD address for network={Network}, index={Index}", network, index);
                return null;
            }
        }

        /// <summary>
        /// Get BIP44 derivation path for a specific network and index
        /// </summary>
        private string GetDerivationPath(string network, int index)
        {
            // BIP44 standard: m/44'/coin_type'/account'/change/address_index
            // coin_type values from https://github.com/satoshilabs/slips/blob/master/slip-0044.md
            return network switch
            {
                "bitcoin" or "btc" => $"m/44'/0'/0'/0/{index}",           // Bitcoin
                "ethereum" or "eth" => $"m/44'/60'/0'/0/{index}",         // Ethereum
                "sepolia" => $"m/44'/60'/0'/0/{index}",                   // Sepolia (uses Ethereum coin type)
                "tron" or "trx" => $"m/44'/60'/0'/0/{index}",             // Tron (uses Ethereum derivation)
                "binance-smart-chain" or "bsc" or "bnb" => $"m/44'/60'/0'/0/{index}", // BSC (EVM compatible)
                "polygon" or "matic" => $"m/44'/60'/0'/0/{index}",        // Polygon (EVM compatible)
                "solana" or "sol" => $"m/44'/501'/0'/0/{index}",          // Solana
                _ => $"m/44'/60'/0'/0/{index}"                             // Default to Ethereum
            };
        }

        /// <summary>
        /// Derive address from master key using specified path
        /// </summary>
        private string? DeriveFromMasterKey(ExtKey masterKey, string derivationPath, string network, int index)
        {
            var path = new NBitcoinKeyPath(derivationPath);
            var childKey = masterKey.Derive(path);
            var privateKeyBytes = childKey.PrivateKey.ToBytes();
            var privateKeyHex = ToHex(privateKeyBytes);

            // For Bitcoin, use native Bitcoin address format
            if (network == "bitcoin" || network == "btc")
            {
                var bitcoinAddress = childKey.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);
                _logger.LogInformation("Derived Bitcoin address at index {Index}: {Address}", index, bitcoinAddress);
                return bitcoinAddress.ToString();
            }

            // For EVM-compatible chains (Ethereum, BSC, Polygon, Sepolia)
            var account = new Account(privateKeyHex);
            var ethereumAddress = account.Address;

            // For Tron, convert Ethereum-style address to Tron base58 format
            if (network == "tron" || network == "trx")
            {
                try
                {
                    // Tron addresses start with 0x41 prefix followed by Ethereum address without 0x
                    var tronHex = "41" + ethereumAddress.Substring(2);
                    var tronAddress = TronAddressConverter.HexToBase58(tronHex);
                    
                    _logger.LogInformation("Derived Tron address at index {Index}: {Address} (from ETH: {EthAddress})", 
                        index, tronAddress, ethereumAddress);
                    
                    return tronAddress;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert Ethereum address to Tron format");
                    return null;
                }
            }

            // Return Ethereum-style address for all other EVM chains
            _logger.LogInformation("Derived {Network} address at index {Index}: {Address}", 
                network, index, ethereumAddress);
            
            return ethereumAddress;
        }

        /// <summary>
        /// Convert byte array to hexadecimal string
        /// </summary>
        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }
    }
}
