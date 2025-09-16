using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services
{
    public class UserVoucherService : IUserVoucherService
    {
        private readonly ApplicationDbContext _context;

        public UserVoucherService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> SaveVoucherAsync(Guid userId, SaveVoucherDTO saveVoucherDto)
        {
            var userVoucher = new UserVoucher
            {
                UserVoucherID = Guid.NewGuid(),
                UserID = userId,
                VoucherID = saveVoucherDto.VoucherID,
                IsUsed = false,
                SavedAt = DateTime.UtcNow
            };

            _context.UserVouchers.Add(userVoucher);
            await _context.SaveChangesAsync();

            return userVoucher.UserVoucherID;
        }

        public async Task<UserVoucherListResponseDTO> GetUserVouchersByUserIdAsync(Guid userId, int page = 1, int size = 10)
        {
            var query = _context.UserVouchers
                .Include(uv => uv.User)
                .Include(uv => uv.Voucher)
                .Where(uv => uv.UserID == userId)
                .OrderByDescending(uv => uv.SavedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var userVouchers = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(uv => new UserVoucherResponseDTO
                {
                    UserVoucherID = uv.UserVoucherID,
                    UserID = uv.UserID,
                    VoucherID = uv.VoucherID,
                    IsUsed = uv.IsUsed,
                    SavedAt = uv.SavedAt,
                    UserName = uv.User!.FullName,
                    VoucherCode = uv.Voucher!.Code,
                    VoucherPercent = uv.Voucher.Percent,
                    VoucherTableType = uv.Voucher.TableType,
                    VoucherStartDate = uv.Voucher.StartDate,
                    VoucherEndDate = uv.Voucher.EndDate,
                    VoucherDescription = uv.Voucher.Description,
                    VoucherIsExpired = DateTime.UtcNow > uv.Voucher.EndDate
                })
                .ToListAsync();

            return new UserVoucherListResponseDTO
            {
                UserVouchers = userVouchers,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        public async Task<UserVoucherListResponseDTO> GetUserVouchersAsync(int page = 1, int size = 10)
        {
            var query = _context.UserVouchers
                .Include(uv => uv.User)
                .Include(uv => uv.Voucher)
                .OrderByDescending(uv => uv.SavedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var userVouchers = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(uv => new UserVoucherResponseDTO
                {
                    UserVoucherID = uv.UserVoucherID,
                    UserID = uv.UserID,
                    VoucherID = uv.VoucherID,
                    IsUsed = uv.IsUsed,
                    SavedAt = uv.SavedAt,
                    UserName = uv.User!.FullName,
                    VoucherCode = uv.Voucher!.Code,
                    VoucherPercent = uv.Voucher.Percent,
                    VoucherTableType = uv.Voucher.TableType,
                    VoucherStartDate = uv.Voucher.StartDate,
                    VoucherEndDate = uv.Voucher.EndDate,
                    VoucherDescription = uv.Voucher.Description,
                    VoucherIsExpired = DateTime.UtcNow > uv.Voucher.EndDate
                })
                .ToListAsync();

            return new UserVoucherListResponseDTO
            {
                UserVouchers = userVouchers,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }


        public async Task<bool> IsVoucherAlreadySavedAsync(Guid userId, Guid voucherId)
        {
            return await _context.UserVouchers
                .AnyAsync(uv => uv.UserID == userId && uv.VoucherID == voucherId);
        }

        
    }
}
