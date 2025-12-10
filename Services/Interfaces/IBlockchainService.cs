namespace Wihngo.Services.Interfaces;

public class TransactionInfo
{
    public decimal Amount { get; set; }
    public string ToAddress { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public int Confirmations { get; set; }
    public long? BlockNumber { get; set; }
    public string? BlockHash { get; set; }
    public decimal? Fee { get; set; }
}

public interface IBlockchainService
{
    Task<TransactionInfo?> VerifyTransactionAsync(string txHash, string currency, string network);
}
