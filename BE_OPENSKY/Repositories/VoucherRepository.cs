using BE_OPENSKY.Data;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Repositories;

// Repository cho Voucher - Xử lý dữ liệu mã giảm giá
public class VoucherRepository : IVoucherRepository
{
    private readonly ApplicationDbContext _context;

    public VoucherRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===== QUẢN LÝ VOUCHER (ADMIN) =====

    // Lấy tất cả voucher
    public async Task<IEnumerable<Voucher>> GetAllAsync()
    {
        return await _context.Vouchers
            .Include(v => v.UserVouchers) // Bao gồm thông tin user đã lưu
                .ThenInclude(uv => uv.User) // Bao gồm thông tin chi tiết user
            .OrderByDescending(v => v.StartDate) // Sắp xếp theo ngày bắt đầu
            .ToListAsync();
    }

    // Lấy voucher theo ID
    public async Task<Voucher?> GetByIdAsync(Guid voucherId)
    {
        return await _context.Vouchers
            .Include(v => v.UserVouchers)
                .ThenInclude(uv => uv.User)
            .FirstOrDefaultAsync(v => v.VoucherID == voucherId);
    }

    // Lấy voucher theo mã code
    public async Task<Voucher?> GetByCodeAsync(string code)
    {
        return await _context.Vouchers
            .Include(v => v.UserVouchers)
                .ThenInclude(uv => uv.User)
            .FirstOrDefaultAsync(v => v.Code == code);
    }

    // Lấy voucher theo loại (Tour hoặc Hotel)
    public async Task<IEnumerable<Voucher>> GetByTableTypeAsync(string tableType)
    {
        return await _context.Vouchers
            .Include(v => v.UserVouchers)
            .Where(v => v.TableType == tableType)
            .OrderByDescending(v => v.StartDate)
            .ToListAsync();
    }

    // Lấy voucher đang có hiệu lực
    public async Task<IEnumerable<Voucher>> GetActiveVouchersAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Vouchers
            .Include(v => v.UserVouchers)
            .Where(v => v.StartDate <= now && v.EndDate >= now) // Trong thời gian hiệu lực
            .OrderByDescending(v => v.StartDate)
            .ToListAsync();
    }

    // Lấy voucher đã hết hạn
    public async Task<IEnumerable<Voucher>> GetExpiredVouchersAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Vouchers
            .Include(v => v.UserVouchers)
            .Where(v => v.EndDate < now) // Đã hết hạn
            .OrderByDescending(v => v.EndDate)
            .ToListAsync();
    }

    // Tạo voucher mới
    public async Task<Voucher> CreateAsync(Voucher voucher)
    {
        voucher.VoucherID = Guid.NewGuid(); // Tạo ID mới
        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();
        return voucher;
    }

    // Cập nhật voucher
    public async Task<Voucher?> UpdateAsync(Voucher voucher)
    {
        _context.Vouchers.Update(voucher);
        var result = await _context.SaveChangesAsync();
        return result > 0 ? voucher : null;
    }

    // Xóa voucher
    public async Task<bool> DeleteAsync(Guid voucherId)
    {
        var voucher = await _context.Vouchers.FindAsync(voucherId);
        if (voucher == null) return false;

        _context.Vouchers.Remove(voucher);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    // Kiểm tra voucher có tồn tại không
    public async Task<bool> ExistsAsync(Guid voucherId)
    {
        return await _context.Vouchers.AnyAsync(v => v.VoucherID == voucherId);
    }

    // Kiểm tra mã voucher có tồn tại không
    public async Task<bool> CodeExistsAsync(string code, Guid? excludeVoucherId = null)
    {
        var query = _context.Vouchers.Where(v => v.Code == code);
        if (excludeVoucherId.HasValue)
        {
            query = query.Where(v => v.VoucherID != excludeVoucherId.Value);
        }
        return await query.AnyAsync();
    }

    // ===== QUẢN LÝ VOUCHER CỦA KHÁCH HÀNG =====

    // Đếm số lần voucher đã được sử dụng
    public async Task<int> GetUsedCountAsync(Guid voucherId)
    {
        return await _context.UserVouchers
            .CountAsync(uv => uv.VoucherID == voucherId && uv.IsUsed);
    }

    // Lấy danh sách user đã lưu voucher
    public async Task<IEnumerable<UserVoucher>> GetUserVouchersAsync(Guid voucherId)
    {
        return await _context.UserVouchers
            .Include(uv => uv.User)
            .Include(uv => uv.Voucher)
            .Where(uv => uv.VoucherID == voucherId)
            .OrderByDescending(uv => uv.SavedAt) // Sắp xếp theo ngày lưu
            .ToListAsync();
    }

    // Khách hàng lưu voucher vào tài khoản
    public async Task<UserVoucher?> SaveVoucherForUserAsync(Guid voucherId, int userId)
    {
        // Kiểm tra user đã lưu voucher này chưa
        var existingUserVoucher = await _context.UserVouchers
            .FirstOrDefaultAsync(uv => uv.VoucherID == voucherId && uv.UserID == userId);

        if (existingUserVoucher != null)
            return null; // Đã lưu rồi, không lưu lại

        var userVoucher = new UserVoucher
        {
            UserVoucherID = Guid.NewGuid(),
            VoucherID = voucherId,
            UserID = userId,
            IsUsed = false,
            SavedAt = DateTime.UtcNow
        };

        _context.UserVouchers.Add(userVoucher);
        await _context.SaveChangesAsync();

        // Trả về với thông tin đầy đủ
        return await _context.UserVouchers
            .Include(uv => uv.User)
            .Include(uv => uv.Voucher)
            .FirstAsync(uv => uv.UserVoucherID == userVoucher.UserVoucherID);
    }

    // Xóa voucher đã lưu của user
    public async Task<bool> RemoveUserVoucherAsync(Guid userVoucherId)
    {
        var userVoucher = await _context.UserVouchers.FindAsync(userVoucherId);
        if (userVoucher == null) return false;

        _context.UserVouchers.Remove(userVoucher);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    // Đánh dấu voucher đã sử dụng
    public async Task<bool> MarkVoucherAsUsedAsync(Guid userVoucherId)
    {
        var userVoucher = await _context.UserVouchers.FindAsync(userVoucherId);
        if (userVoucher == null) return false;

        userVoucher.IsUsed = true;
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }

    // Lấy tất cả voucher đã lưu của user
    public async Task<IEnumerable<UserVoucher>> GetUserSavedVouchersAsync(int userId)
    {
        return await _context.UserVouchers
            .Include(uv => uv.Voucher)
            .Include(uv => uv.User)
            .Where(uv => uv.UserID == userId)
            .OrderByDescending(uv => uv.SavedAt) // Sắp xếp theo ngày lưu
            .ToListAsync();
    }

    // Kiểm tra user đã lưu voucher này chưa
    public async Task<bool> HasUserSavedVoucherAsync(Guid voucherId, int userId)
    {
        return await _context.UserVouchers
            .AnyAsync(uv => uv.VoucherID == voucherId && uv.UserID == userId);
    }

    // Kiểm tra voucher có thể sử dụng không (còn hiệu lực và chưa hết lượt)
    public async Task<bool> CanUseVoucherAsync(Guid voucherId)
    {
        var voucher = await _context.Vouchers
            .Include(v => v.UserVouchers)
            .FirstOrDefaultAsync(v => v.VoucherID == voucherId);

        if (voucher == null) return false;

        var now = DateTime.UtcNow;
        // Kiểm tra còn hiệu lực
        if (voucher.StartDate > now || voucher.EndDate < now)
            return false;

        // Kiểm tra còn lượt sử dụng
        var usedCount = voucher.UserVouchers.Count(uv => uv.IsUsed);
        return usedCount < voucher.MaxUsage;
    }
}