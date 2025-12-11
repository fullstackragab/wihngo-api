using Wihngo.Dtos;
using Wihngo.Models.Entities;

namespace Wihngo.Services.Interfaces;

public interface ICryptoPaymentService
{
    Task<PaymentResponseDto> CreatePaymentRequestAsync(Guid userId, CreatePaymentRequestDto dto);
    Task<PaymentResponseDto?> GetPaymentRequestAsync(Guid paymentId, Guid userId);
    Task<PaymentResponseDto> VerifyPaymentAsync(Guid paymentId, Guid userId, VerifyPaymentDto dto);
    Task<List<PaymentResponseDto>> GetPaymentHistoryAsync(Guid userId, int page, int pageSize);
    Task<PlatformWallet?> GetPlatformWalletAsync(string currency, string network);
    Task CompletePaymentAsync(CryptoPaymentRequest payment);
    Task<PaymentResponseDto?> CancelPaymentAsync(Guid paymentId, Guid userId);
}
