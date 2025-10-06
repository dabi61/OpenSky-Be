using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly ApplicationDbContext _context;

        public VoucherService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateVoucherAsync(CreateVoucherDTO createVoucherDto)
        {
            var voucher = new Voucher
            {
                VoucherID = Guid.NewGuid(),
                Code = createVoucherDto.Code,
                Percent = createVoucherDto.Percent,
                TableType = createVoucherDto.TableType,
                StartDate = createVoucherDto.StartDate,
                EndDate = createVoucherDto.EndDate,
                Description = createVoucherDto.Description,
                // MaxUsage: không nhận từ API, giữ nguyên mặc định DB nếu có
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            return voucher.VoucherID;
        }

        public async Task<VoucherResponseDTO?> GetVoucherByIdAsync(Guid voucherId)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.UserVouchers)
                .FirstOrDefaultAsync(v => v.VoucherID == voucherId && !v.IsDeleted);

            if (voucher == null) return null;

            var usedCount = voucher.UserVouchers.Count(uv => uv.IsUsed);
            var isExpired = DateTime.UtcNow > voucher.EndDate;
            var isAvailable = !isExpired; // Chỉ dựa vào thời gian

            return new VoucherResponseDTO
            {
                VoucherID = voucher.VoucherID,
                Code = voucher.Code,
                Percent = voucher.Percent,
                TableType = voucher.TableType,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                Description = voucher.Description,
                IsDeleted = voucher.IsDeleted,
                UsedCount = usedCount,
                IsExpired = isExpired,
                IsAvailable = isAvailable
            };
        }

        public async Task<VoucherListResponseDTO> GetVouchersAsync(int page = 1, int size = 10)
        {
            var query = _context.Vouchers
                .Include(v => v.UserVouchers)
                .Where(v => !v.IsDeleted)
                .OrderByDescending(v => v.StartDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var vouchers = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(v => new VoucherResponseDTO
                {
                    VoucherID = v.VoucherID,
                    Code = v.Code,
                    Percent = v.Percent,
                    TableType = v.TableType,
                    StartDate = v.StartDate,
                    EndDate = v.EndDate,
                    Description = v.Description,
                    IsDeleted = v.IsDeleted,
                    UsedCount = v.UserVouchers.Count(uv => uv.IsUsed),
                    IsExpired = DateTime.UtcNow > v.EndDate,
                    IsAvailable = !(DateTime.UtcNow > v.EndDate)
                })
                .ToListAsync();

            return new VoucherListResponseDTO
            {
                Vouchers = vouchers,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        public async Task<VoucherListResponseDTO> GetVouchersByTableTypeAsync(TableType tableType, int page = 1, int size = 10)
        {
            var query = _context.Vouchers
                .Include(v => v.UserVouchers)
                .Where(v => v.TableType == tableType)
                .OrderByDescending(v => v.StartDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var vouchers = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(v => new VoucherResponseDTO
                {
                    VoucherID = v.VoucherID,
                    Code = v.Code,
                    Percent = v.Percent,
                    TableType = v.TableType,
                    StartDate = v.StartDate,
                    EndDate = v.EndDate,
                    Description = v.Description,
                    IsDeleted = v.IsDeleted,
                    UsedCount = v.UserVouchers.Count(uv => uv.IsUsed),
                    IsExpired = DateTime.UtcNow > v.EndDate,
                    IsAvailable = !(DateTime.UtcNow > v.EndDate)
                })
                .ToListAsync();

            return new VoucherListResponseDTO
            {
                Vouchers = vouchers,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        public async Task<VoucherListResponseDTO> GetActiveVouchersAsync(int page = 1, int size = 10)
        {
            var now = DateTime.UtcNow;
            var query = _context.Vouchers
                .Include(v => v.UserVouchers)
                .Where(v => !v.IsDeleted && v.StartDate <= now && v.EndDate >= now)
                .OrderByDescending(v => v.StartDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var vouchers = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(v => new VoucherResponseDTO
                {
                    VoucherID = v.VoucherID,
                    Code = v.Code,
                    Percent = v.Percent,
                    TableType = v.TableType,
                    StartDate = v.StartDate,
                    EndDate = v.EndDate,
                    Description = v.Description,
                    IsDeleted = v.IsDeleted,
                    UsedCount = v.UserVouchers.Count(uv => uv.IsUsed),
                    IsExpired = false,
                    IsAvailable = true
                })
                .ToListAsync();

            return new VoucherListResponseDTO
            {
                Vouchers = vouchers,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        public async Task<VoucherListResponseDTO> GetActiveVouchersExcludingSavedByUserAsync(Guid userId, int page = 1, int size = 10)
        {
            var now = DateTime.UtcNow;
            var savedVoucherIdsQuery = _context.UserVouchers
                .Where(uv => uv.UserID == userId)
                .Select(uv => uv.VoucherID);

            var query = _context.Vouchers
                .Include(v => v.UserVouchers)
                .Where(v => !v.IsDeleted && v.StartDate <= now && v.EndDate >= now && !savedVoucherIdsQuery.Contains(v.VoucherID))
                .OrderByDescending(v => v.StartDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var vouchers = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(v => new VoucherResponseDTO
                {
                    VoucherID = v.VoucherID,
                    Code = v.Code,
                    Percent = v.Percent,
                    TableType = v.TableType,
                    StartDate = v.StartDate,
                    EndDate = v.EndDate,
                    Description = v.Description,
                    IsDeleted = v.IsDeleted,
                    UsedCount = v.UserVouchers.Count(uv => uv.IsUsed),
                    IsExpired = false,
                    IsAvailable = true
                })
                .ToListAsync();

            return new VoucherListResponseDTO
            {
                Vouchers = vouchers,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        public async Task<bool> UpdateVoucherAsync(Guid voucherId, UpdateVoucherDTO updateVoucherDto)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherID == voucherId && !v.IsDeleted);

            if (voucher == null) return false;

            if (!string.IsNullOrEmpty(updateVoucherDto.Code))
                voucher.Code = updateVoucherDto.Code;

            if (updateVoucherDto.Percent.HasValue)
                voucher.Percent = updateVoucherDto.Percent.Value;

            if (updateVoucherDto.TableType.HasValue)
                voucher.TableType = updateVoucherDto.TableType.Value;

            if (updateVoucherDto.StartDate.HasValue)
                voucher.StartDate = updateVoucherDto.StartDate.Value;

            if (updateVoucherDto.EndDate.HasValue)
                voucher.EndDate = updateVoucherDto.EndDate.Value;

            if (updateVoucherDto.Description != null)
                voucher.Description = updateVoucherDto.Description;


            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteVoucherAsync(Guid voucherId)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherID == voucherId && !v.IsDeleted);

            if (voucher == null) return false;

            voucher.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsVoucherCodeExistsAsync(string code)
        {
            return await _context.Vouchers
                .AnyAsync(v => v.Code == code && !v.IsDeleted);
        }

        public async Task<VoucherListResponseDTO> SearchVouchersForAdminAsync(AdminVoucherSearchDTO searchDto)
        {
            var query = _context.Vouchers
                .Include(v => v.UserVouchers)
                .Where(v => !v.IsDeleted)
                .AsQueryable();

            // Tìm kiếm theo keyword (code voucher, không phân biệt chữ hoa/thường)
            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                query = query.Where(v => EF.Functions.Like(v.Code.ToLower(), $"%{searchDto.Keyword.ToLower()}%"));
            }

            // Lọc theo type (nếu có)
            if (searchDto.Type.HasValue)
            {
                query = query.Where(v => v.TableType == searchDto.Type.Value);
            }

            // Sắp xếp theo ngày tạo (mới nhất trước)
            query = query.OrderByDescending(v => v.StartDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / searchDto.Size);

            var vouchers = await query
                .Skip((searchDto.Page - 1) * searchDto.Size)
                .Take(searchDto.Size)
                .Select(v => new VoucherResponseDTO
                {
                    VoucherID = v.VoucherID,
                    Code = v.Code,
                    Percent = v.Percent,
                    TableType = v.TableType,
                    StartDate = v.StartDate,
                    EndDate = v.EndDate,
                    Description = v.Description,
                    IsDeleted = v.IsDeleted,
                    UsedCount = v.UserVouchers.Count(uv => uv.IsUsed),
                    IsExpired = DateTime.UtcNow > v.EndDate,
                    IsAvailable = !(DateTime.UtcNow > v.EndDate)
                })
                .ToListAsync();

            return new VoucherListResponseDTO
            {
                Vouchers = vouchers,
                TotalCount = totalCount,
                Page = searchDto.Page,
                Size = searchDto.Size,
                TotalPages = totalPages
            };
        }
    }
}
