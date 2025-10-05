using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IRefundService
    {
        // Tạo refund request
        Task<Guid> CreateRefundAsync(Guid userId, CreateRefundDTO createRefundDto);
        // Tạo yêu cầu refund dạng chờ duyệt (không thay đổi DB bill/booking)
        Task<Guid> CreateRefundRequestAsync(Guid userId, CreateRefundDTO createRefundDto);
        // Duyệt yêu cầu refund (thực hiện hoàn tiền và ghi DB như hiện tại)
        Task<Guid> ApproveRefundRequestAsync(Guid billId, Guid approverId);
        // Từ chối yêu cầu refund (xóa yêu cầu pending ở Redis)
        Task<bool> RejectRefundRequestAsync(Guid billId, Guid approverId, string? reason = null);
        
        // Lấy refund theo ID
        Task<RefundResponseDTO?> GetRefundByIdAsync(Guid refundId, Guid userId);

        Task<RefundResponseDTO?> GetRefundByBillIdAsync(Guid billId, Guid userId);

        // Lấy danh sách refund của user
        Task<RefundListResponseDTO> GetUserRefundsAsync(Guid userId, int page = 1, int size = 10);
        
        // Lấy tất cả refund (Admin/Manager)
        Task<RefundListResponseDTO> GetAllRefundsAsync(int page = 1, int size = 10);
        
        // Lấy danh sách refund theo status
        Task<RefundListResponseDTO> GetRefundsByStatusAsync(RefundStatus status, int page = 1, int size = 10);
        
        // Lấy thống kê refund
        Task<RefundStatsDTO> GetRefundStatsAsync();
        
    }
}
