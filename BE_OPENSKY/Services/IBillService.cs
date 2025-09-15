using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IBillService
    {
        // Lấy bill theo ID
        Task<BillResponseDTO?> GetBillByIdAsync(Guid billId, Guid userId);
        
        // Lấy bills của user
        Task<List<BillResponseDTO>> GetUserBillsAsync(Guid userId);
        
        // Cập nhật trạng thái bill khi thanh toán thành công
        Task<bool> UpdateBillPaymentStatusAsync(Guid billId, string paymentMethod, string transactionId, decimal amount);
        
        // Lấy bill theo booking ID
        Task<BillResponseDTO?> GetBillByBookingIdAsync(Guid bookingId);
        
        // Áp dụng voucher vào bill đã có
        Task<ApplyVoucherResponseDTO> ApplyVoucherToBillAsync(Guid billId, Guid userId, ApplyVoucherToBillDTO applyVoucherDto);
        
        // Xóa voucher khỏi bill
        Task<ApplyVoucherResponseDTO> RemoveVoucherFromBillAsync(Guid billId, Guid userId);
    }
}
