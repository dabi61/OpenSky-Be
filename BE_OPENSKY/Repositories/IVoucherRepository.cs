using BE_OPENSKY.Models;

namespace BE_OPENSKY.Repositories;

// Interface Repository cho Voucher
public interface IVoucherRepository
{
    // Quản lý voucher (Admin)
    Task<IEnumerable<Voucher>> GetAllAsync(); // Lấy tất cả voucher
    Task<Voucher?> GetByIdAsync(Guid voucherId); // Lấy voucher theo ID
    Task<Voucher?> GetByCodeAsync(string code); // Lấy voucher theo mã
    Task<IEnumerable<Voucher>> GetByTableTypeAsync(string tableType); // Lấy voucher theo loại
    Task<IEnumerable<Voucher>> GetActiveVouchersAsync(); // Lấy voucher đang hiệu lực
    Task<IEnumerable<Voucher>> GetExpiredVouchersAsync(); // Lấy voucher hết hạn
    Task<Voucher> CreateAsync(Voucher voucher); // Tạo voucher mới
    Task<Voucher?> UpdateAsync(Voucher voucher); // Cập nhật voucher
    Task<bool> DeleteAsync(Guid voucherId); // Xóa voucher
    Task<bool> ExistsAsync(Guid voucherId); // Kiểm tra voucher tồn tại
    Task<bool> CodeExistsAsync(string code, Guid? excludeVoucherId = null); // Kiểm tra mã voucher tồn tại
    
    // Quản lý voucher của khách hàng
    Task<int> GetUsedCountAsync(Guid voucherId); // Đếm số lần đã sử dụng
    Task<IEnumerable<UserVoucher>> GetUserVouchersAsync(Guid voucherId); // Lấy danh sách user đã lưu voucher
    Task<UserVoucher?> SaveVoucherForUserAsync(Guid voucherId, int userId); // Khách hàng lưu voucher
    Task<bool> RemoveUserVoucherAsync(Guid userVoucherId); // Xóa voucher đã lưu
    Task<bool> MarkVoucherAsUsedAsync(Guid userVoucherId); // Đánh dấu voucher đã sử dụng
    Task<IEnumerable<UserVoucher>> GetUserSavedVouchersAsync(int userId); // Lấy voucher đã lưu của user
    Task<bool> HasUserSavedVoucherAsync(Guid voucherId, int userId); // Kiểm tra user đã lưu voucher chưa
    Task<bool> CanUseVoucherAsync(Guid voucherId); // Kiểm tra voucher có thể sử dụng không
}
