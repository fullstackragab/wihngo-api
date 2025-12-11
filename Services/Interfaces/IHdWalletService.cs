namespace Wihngo.Services.Interfaces
{
    public interface IHdWalletService
    {
        string? DeriveAddress(string mnemonicOrXprv, string network, int index = 0);
    }
}
