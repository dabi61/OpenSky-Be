using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IVNPayService
    {
        // Tạo URL thanh toán VNPay
        Task<VNPayPaymentResponseDTO> CreatePaymentUrlAsync(VNPayPaymentRequestDTO request);
        
        // Xử lý callback từ VNPay
        Task<PaymentResultDTO> ProcessCallbackAsync(VNPayCallbackDTO callback);
        
        // Kiểm tra trạng thái thanh toán
        Task<PaymentResultDTO> CheckPaymentStatusAsync(string transactionId);
    }
}
