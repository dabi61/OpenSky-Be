using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IRefundService
    {
        // Tạo refund request
        Task<Guid> CreateRefundAsync(Guid userId, CreateRefundDTO createRefundDto);
        
        // Lấy refund theo ID
        Task<RefundResponseDTO?> GetRefundByIdAsync(Guid refundId, Guid userId);
        
        // Lấy danh sách refund của user
        Task<RefundListResponseDTO> GetUserRefundsAsync(Guid userId, int page = 1, int size = 10);
        
        // Lấy tất cả refund (Admin/Manager)
        Task<RefundListResponseDTO> GetAllRefundsAsync(int page = 1, int size = 10);
        
        
        // Lấy thống kê refund
        Task<RefundStatsDTO> GetRefundStatsAsync();
        
    }
}
