using System.Numerics;
using System.Security.Cryptography;

namespace Wihngo.Services;

/// <summary>
/// Helper class for TRON address conversion
/// </summary>
public static class TronAddressConverter
{
    private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    /// <summary>
    /// Convert TRON hex address to Base58 address
    /// </summary>
    public static string HexToBase58(string hexAddress)
    {
        if (string.IsNullOrEmpty(hexAddress))
            return string.Empty;

        // Remove 0x prefix if present
        if (hexAddress.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hexAddress = hexAddress[2..];

        try
        {
            // Convert hex to bytes
            var addressBytes = Convert.FromHexString(hexAddress);

            // Calculate checksum (double SHA256 of address bytes)
            var hash1 = SHA256.HashData(addressBytes);
            var hash2 = SHA256.HashData(hash1);

            // Take first 4 bytes as checksum
            var checksum = hash2[..4];

            // Combine address + checksum
            var addressWithChecksum = new byte[addressBytes.Length + 4];
            Buffer.BlockCopy(addressBytes, 0, addressWithChecksum, 0, addressBytes.Length);
            Buffer.BlockCopy(checksum, 0, addressWithChecksum, addressBytes.Length, 4);

            // Convert to Base58
            return EncodeBase58(addressWithChecksum);
        }
        catch
        {
            return hexAddress; // Return original if conversion fails
        }
    }

    /// <summary>
    /// Encode bytes to Base58 string
    /// </summary>
    private static string EncodeBase58(byte[] data)
    {
        BigInteger intData = 0;
        for (int i = 0; i < data.Length; i++)
        {
            intData = intData * 256 + data[i];
        }

        var result = string.Empty;
        while (intData > 0)
        {
            var remainder = (int)(intData % 58);
            intData /= 58;
            result = Base58Alphabet[remainder] + result;
        }

        // Add leading zeros
        for (int i = 0; i < data.Length && data[i] == 0; i++)
        {
            result = '1' + result;
        }

        return result;
    }

    /// <summary>
    /// Validate if string is a valid TRON Base58 address
    /// </summary>
    public static bool IsValidTronAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return false;

        // TRON addresses start with 'T' and are 34 characters long
        return address.Length == 34 && address[0] == 'T';
    }
}
