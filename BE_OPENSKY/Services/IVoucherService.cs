using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services;

// Interface Service cho Voucher
public interface IVoucherService
{
    // Quản lý voucher (Admin)
    Task<IEnumerable<VoucherResponseDTO>> GetAllAsync(); // Lấy tất cả voucher
    Task<VoucherResponseDTO?> GetByIdAsync(Guid voucherId); // Lấy voucher theo ID
    Task<VoucherResponseDTO?> GetByCodeAsync(string code); // Lấy voucher theo mã
    Task<IEnumerable<VoucherResponseDTO>> GetByTableTypeAsync(string tableType); // Lấy voucher theo loại
    Task<IEnumerable<VoucherResponseDTO>> GetActiveVouchersAsync(); // Lấy voucher đang hiệu lực
    Task<IEnumerable<VoucherResponseDTO>> GetExpiredVouchersAsync(); // Lấy voucher hết hạn
    Task<VoucherResponseDTO> CreateAsync(VoucherCreateDTO voucherDto); // Tạo voucher mới
    Task<VoucherResponseDTO?> UpdateAsync(Guid voucherId, VoucherUpdateDTO voucherDto); // Cập nhật voucher
    Task<bool> DeleteAsync(Guid voucherId); // Xóa voucher
    Task<VoucherStatisticsDTO> GetStatisticsAsync(); // Thống kê voucher

    // Quản lý voucher của khách hàng
    Task<IEnumerable<UserVoucherResponseDTO>> GetVoucherUsersAsync(Guid voucherId); // Lấy user đã lưu voucher
    Task<UserVoucherResponseDTO?> SaveVoucherAsync(string code, int userId); // Khách hàng lưu voucher
    Task<bool> RemoveUserVoucherAsync(Guid userVoucherId); // Xóa voucher đã lưu
    Task<bool> MarkVoucherAsUsedAsync(Guid userVoucherId); // Đánh dấu đã sử dụng
    Task<IEnumerable<UserVoucherResponseDTO>> GetUserSavedVouchersAsync(int userId); // Lấy voucher đã lưu của user
    
    // Validation
    Task<bool> ValidateVoucherForTableAsync(int tableId, string tableType); // Validate Tour/Hotel tồn tại
}
