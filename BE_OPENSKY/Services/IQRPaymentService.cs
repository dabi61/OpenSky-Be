using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IQRPaymentService
    {
        // Tạo QR code thanh toán
        Task<QRPaymentResponseDTO> CreateQRPaymentAsync(QRPaymentRequestDTO request);
        
        // Quét QR code để thanh toán (mô phỏng)
        Task<QRPaymentStatusDTO> ScanQRPaymentAsync(string qrCode);
        
        // Kiểm tra trạng thái thanh toán
        Task<QRPaymentStatusDTO> GetPaymentStatusAsync(Guid billId);
    }
}
