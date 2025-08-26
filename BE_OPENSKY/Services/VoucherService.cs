using AutoMapper;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using BE_OPENSKY.Repositories;

namespace BE_OPENSKY.Services;

// Service cho Voucher - Xử lý logic nghiệp vụ mã giảm giá
public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _voucherRepository;
    private readonly ITourRepository _tourRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public VoucherService(
        IVoucherRepository voucherRepository,
        ITourRepository tourRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _voucherRepository = voucherRepository;
        _tourRepository = tourRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    // ===== QUẢN LÝ VOUCHER (ADMIN) =====

    // Lấy tất cả voucher
    public async Task<IEnumerable<VoucherResponseDTO>> GetAllAsync()
    {
        var vouchers = await _voucherRepository.GetAllAsync();
        var voucherDtos = new List<VoucherResponseDTO>();

        foreach (var voucher in vouchers)
        {
            var dto = await MapToResponseDTOAsync(voucher);
            voucherDtos.Add(dto);
        }

        return voucherDtos;
    }

    // Lấy voucher theo ID
    public async Task<VoucherResponseDTO?> GetByIdAsync(Guid voucherId)
    {
        var voucher = await _voucherRepository.GetByIdAsync(voucherId);
        if (voucher == null) return null;

        return await MapToResponseDTOAsync(voucher);
    }

    // Lấy voucher theo mã code
    public async Task<VoucherResponseDTO?> GetByCodeAsync(string code)
    {
        var voucher = await _voucherRepository.GetByCodeAsync(code);
        if (voucher == null) return null;

        return await MapToResponseDTOAsync(voucher);
    }

    // Lấy voucher theo loại (Tour/Hotel)
    public async Task<IEnumerable<VoucherResponseDTO>> GetByTableTypeAsync(string tableType)
    {
        var vouchers = await _voucherRepository.GetByTableTypeAsync(tableType);
        var voucherDtos = new List<VoucherResponseDTO>();

        foreach (var voucher in vouchers)
        {
            var dto = await MapToResponseDTOAsync(voucher);
            voucherDtos.Add(dto);
        }

        return voucherDtos;
    }

    // Lấy voucher đang có hiệu lực
    public async Task<IEnumerable<VoucherResponseDTO>> GetActiveVouchersAsync()
    {
        var vouchers = await _voucherRepository.GetActiveVouchersAsync();
        var voucherDtos = new List<VoucherResponseDTO>();

        foreach (var voucher in vouchers)
        {
            var dto = await MapToResponseDTOAsync(voucher);
            voucherDtos.Add(dto);
        }

        return voucherDtos;
    }

    // Lấy voucher đã hết hạn
    public async Task<IEnumerable<VoucherResponseDTO>> GetExpiredVouchersAsync()
    {
        var vouchers = await _voucherRepository.GetExpiredVouchersAsync();
        var voucherDtos = new List<VoucherResponseDTO>();

        foreach (var voucher in vouchers)
        {
            var dto = await MapToResponseDTOAsync(voucher);
            voucherDtos.Add(dto);
        }

        return voucherDtos;
    }

    // Tạo voucher mới
    public async Task<VoucherResponseDTO> CreateAsync(VoucherCreateDTO voucherDto)
    {
        // Validate dữ liệu đầu vào
        await ValidateCreateVoucherAsync(voucherDto);

        var voucher = new Voucher
        {
            Code = voucherDto.Code,
            Percent = voucherDto.Percent,
            TableType = voucherDto.TableType,
            TableID = voucherDto.TableID,
            StartDate = voucherDto.StartDate.ToUniversalTime(),
            EndDate = voucherDto.EndDate.ToUniversalTime(),
            Description = voucherDto.Description,
            MaxUsage = voucherDto.MaxUsage,
            UserVouchers = new List<UserVoucher>()
        };

        var createdVoucher = await _voucherRepository.CreateAsync(voucher);
        return await MapToResponseDTOAsync(createdVoucher);
    }

    // Cập nhật voucher
    public async Task<VoucherResponseDTO?> UpdateAsync(Guid voucherId, VoucherUpdateDTO voucherDto)
    {
        var existingVoucher = await _voucherRepository.GetByIdAsync(voucherId);
        if (existingVoucher == null) return null;

        // Validate dữ liệu cập nhật
        await ValidateUpdateVoucherAsync(existingVoucher, voucherDto);

        // Cập nhật các trường được cung cấp
        if (!string.IsNullOrEmpty(voucherDto.Code))
            existingVoucher.Code = voucherDto.Code;
        if (voucherDto.Percent.HasValue)
            existingVoucher.Percent = voucherDto.Percent.Value;
        if (voucherDto.StartDate.HasValue)
            existingVoucher.StartDate = voucherDto.StartDate.Value.ToUniversalTime();
        if (voucherDto.EndDate.HasValue)
            existingVoucher.EndDate = voucherDto.EndDate.Value.ToUniversalTime();
        if (voucherDto.Description != null)
            existingVoucher.Description = voucherDto.Description;
        if (voucherDto.MaxUsage.HasValue)
            existingVoucher.MaxUsage = voucherDto.MaxUsage.Value;

        var updatedVoucher = await _voucherRepository.UpdateAsync(existingVoucher);
        if (updatedVoucher == null) return null;

        return await MapToResponseDTOAsync(updatedVoucher);
    }

    // Xóa voucher
    public async Task<bool> DeleteAsync(Guid voucherId)
    {
        var voucher = await _voucherRepository.GetByIdAsync(voucherId);
        if (voucher == null) return false;

        // Kiểm tra voucher đã được sử dụng chưa
        var usedCount = await _voucherRepository.GetUsedCountAsync(voucherId);
        if (usedCount > 0)
        {
            throw new InvalidOperationException("Không thể xóa voucher đã được khách hàng sử dụng");
        }

        return await _voucherRepository.DeleteAsync(voucherId);
    }

    // Thống kê voucher
    public async Task<VoucherStatisticsDTO> GetStatisticsAsync()
    {
        var allVouchers = await _voucherRepository.GetAllAsync();
        var activeVouchers = await _voucherRepository.GetActiveVouchersAsync();
        var expiredVouchers = await _voucherRepository.GetExpiredVouchersAsync();

        var tourVouchers = allVouchers.Count(v => v.TableType == "Tour");
        var hotelVouchers = allVouchers.Count(v => v.TableType == "Hotel");

        var totalSaved = allVouchers.Sum(v => v.UserVouchers.Count);
        var totalUsed = allVouchers.Sum(v => v.UserVouchers.Count(uv => uv.IsUsed));

        return new VoucherStatisticsDTO
        {
            TotalVouchers = allVouchers.Count(),
            ActiveVouchers = activeVouchers.Count(),
            ExpiredVouchers = expiredVouchers.Count(),
            TourVouchers = tourVouchers,
            HotelVouchers = hotelVouchers,
            TotalSaved = totalSaved,
            TotalUsed = totalUsed
        };
    }

    // ===== QUẢN LÝ VOUCHER CỦA KHÁCH HÀNG =====

    // Lấy danh sách user đã lưu voucher
    public async Task<IEnumerable<UserVoucherResponseDTO>> GetVoucherUsersAsync(Guid voucherId)
    {
        var userVouchers = await _voucherRepository.GetUserVouchersAsync(voucherId);
        return userVouchers.Select(uv => new UserVoucherResponseDTO
        {
            UserVoucherID = uv.UserVoucherID,
            UserID = uv.UserID,
            UserFullName = uv.User.FullName,
            UserEmail = uv.User.Email,
            VoucherID = uv.VoucherID,
            VoucherCode = uv.Voucher.Code,
            VoucherPercent = uv.Voucher.Percent,
            VoucherDescription = uv.Voucher.Description ?? "",
            IsUsed = uv.IsUsed,
            SavedAt = uv.SavedAt,
            VoucherEndDate = uv.Voucher.EndDate,
            IsExpired = uv.Voucher.EndDate < DateTime.UtcNow
        });
    }

    // Khách hàng lưu voucher bằng mã code
    public async Task<UserVoucherResponseDTO?> SaveVoucherAsync(string code, int userId)
    {
        // Tìm voucher theo mã
        var voucher = await _voucherRepository.GetByCodeAsync(code);
        if (voucher == null)
            throw new InvalidOperationException("Mã voucher không tồn tại");

        // Kiểm tra voucher còn hiệu lực không
        var now = DateTime.UtcNow;
        if (voucher.StartDate > now || voucher.EndDate < now)
            throw new InvalidOperationException("Voucher không còn hiệu lực");

        // Kiểm tra voucher còn lượt sử dụng không
        var canUse = await _voucherRepository.CanUseVoucherAsync(voucher.VoucherID);
        if (!canUse)
            throw new InvalidOperationException("Voucher đã hết lượt sử dụng");

        // Kiểm tra user có tồn tại không
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Người dùng không tồn tại");

        // Kiểm tra user đã lưu voucher này chưa
        var hasVoucher = await _voucherRepository.HasUserSavedVoucherAsync(voucher.VoucherID, userId);
        if (hasVoucher)
            throw new InvalidOperationException("Bạn đã lưu voucher này rồi");

        // Lưu voucher cho user
        var userVoucher = await _voucherRepository.SaveVoucherForUserAsync(voucher.VoucherID, userId);
        if (userVoucher == null)
            throw new InvalidOperationException("Không thể lưu voucher");

        return new UserVoucherResponseDTO
        {
            UserVoucherID = userVoucher.UserVoucherID,
            UserID = userVoucher.UserID,
            UserFullName = userVoucher.User.FullName,
            UserEmail = userVoucher.User.Email,
            VoucherID = userVoucher.VoucherID,
            VoucherCode = userVoucher.Voucher.Code,
            VoucherPercent = userVoucher.Voucher.Percent,
            VoucherDescription = userVoucher.Voucher.Description ?? "",
            IsUsed = userVoucher.IsUsed,
            SavedAt = userVoucher.SavedAt,
            VoucherEndDate = userVoucher.Voucher.EndDate,
            IsExpired = userVoucher.Voucher.EndDate < DateTime.UtcNow
        };
    }

    // Xóa voucher đã lưu
    public async Task<bool> RemoveUserVoucherAsync(Guid userVoucherId)
    {
        return await _voucherRepository.RemoveUserVoucherAsync(userVoucherId);
    }

    // Đánh dấu voucher đã sử dụng
    public async Task<bool> MarkVoucherAsUsedAsync(Guid userVoucherId)
    {
        return await _voucherRepository.MarkVoucherAsUsedAsync(userVoucherId);
    }

    // Lấy tất cả voucher đã lưu của user
    public async Task<IEnumerable<UserVoucherResponseDTO>> GetUserSavedVouchersAsync(int userId)
    {
        var userVouchers = await _voucherRepository.GetUserSavedVouchersAsync(userId);
        return userVouchers.Select(uv => new UserVoucherResponseDTO
        {
            UserVoucherID = uv.UserVoucherID,
            UserID = uv.UserID,
            UserFullName = uv.User.FullName,
            UserEmail = uv.User.Email,
            VoucherID = uv.VoucherID,
            VoucherCode = uv.Voucher.Code,
            VoucherPercent = uv.Voucher.Percent,
            VoucherDescription = uv.Voucher.Description ?? "",
            IsUsed = uv.IsUsed,
            SavedAt = uv.SavedAt,
            VoucherEndDate = uv.Voucher.EndDate,
            IsExpired = uv.Voucher.EndDate < DateTime.UtcNow
        });
    }

    // ===== VALIDATION =====

    // Validate Tour/Hotel có tồn tại không
    public async Task<bool> ValidateVoucherForTableAsync(int tableId, string tableType)
    {
        if (tableType == "Tour")
        {
            var tour = await _tourRepository.GetByIdAsync(tableId);
            return tour != null;
        }
        else if (tableType == "Hotel")
        {
            // Note: Cần tạo IHotelRepository nếu chưa có
            // Tạm thời return true
            return true;
        }

        return false;
    }

    // ===== PRIVATE METHODS =====

    // Chuyển đổi Voucher entity thành VoucherResponseDTO
    private async Task<VoucherResponseDTO> MapToResponseDTOAsync(Voucher voucher)
    {
        var usedCount = voucher.UserVouchers.Count(uv => uv.IsUsed);
        var now = DateTime.UtcNow;
        var isActive = voucher.StartDate <= now && voucher.EndDate >= now;

        // Lấy tên Tour/Hotel liên kết
        string? relatedItemName = null;
        if (voucher.TableType == "Tour")
        {
            var tour = await _tourRepository.GetByIdAsync(voucher.TableID);
            relatedItemName = tour?.Address; // Dùng Address làm tên tour
        }
        else if (voucher.TableType == "Hotel")
        {
            // Note: Implement khi có IHotelRepository
            relatedItemName = "Hotel"; // Placeholder
        }

        return new VoucherResponseDTO
        {
            VoucherID = voucher.VoucherID,
            Code = voucher.Code,
            Percent = voucher.Percent,
            TableType = voucher.TableType,
            TableID = voucher.TableID,
            StartDate = voucher.StartDate,
            EndDate = voucher.EndDate,
            Description = voucher.Description,
            MaxUsage = voucher.MaxUsage,
            UsedCount = usedCount,
            RemainingUsage = voucher.MaxUsage - usedCount,
            IsActive = isActive,
            RelatedItemName = relatedItemName,
            CreatedAt = DateTime.UtcNow // Note: Có thể thêm CreatedAt vào Voucher model
        };
    }

    // Validate khi tạo voucher mới
    private async Task ValidateCreateVoucherAsync(VoucherCreateDTO voucherDto)
    {
        // Kiểm tra mã voucher đã tồn tại chưa
        if (await _voucherRepository.CodeExistsAsync(voucherDto.Code))
            throw new InvalidOperationException("Mã voucher đã tồn tại");

        // Kiểm tra khoảng thời gian
        if (voucherDto.StartDate >= voucherDto.EndDate)
            throw new InvalidOperationException("Ngày bắt đầu phải trước ngày kết thúc");

        // Kiểm tra phần trăm giảm giá
        if (voucherDto.Percent <= 0 || voucherDto.Percent > 100)
            throw new InvalidOperationException("Phần trăm giảm giá phải từ 1 đến 100");

        // Kiểm tra số lần sử dụng tối đa
        if (voucherDto.MaxUsage <= 0)
            throw new InvalidOperationException("Số lần sử dụng tối đa phải lớn hơn 0");

        // Kiểm tra loại voucher
        if (voucherDto.TableType != "Tour" && voucherDto.TableType != "Hotel")
            throw new InvalidOperationException("Loại voucher phải là 'Tour' hoặc 'Hotel'");

        // Kiểm tra Tour/Hotel có tồn tại không
        var isValidTable = await ValidateVoucherForTableAsync(voucherDto.TableID, voucherDto.TableType);
        if (!isValidTable)
            throw new InvalidOperationException($"{voucherDto.TableType} được chỉ định không tồn tại");
    }

    // Validate khi cập nhật voucher
    private async Task ValidateUpdateVoucherAsync(Voucher existingVoucher, VoucherUpdateDTO voucherDto)
    {
        // Kiểm tra mã voucher nếu có thay đổi
        if (!string.IsNullOrEmpty(voucherDto.Code) && voucherDto.Code != existingVoucher.Code)
        {
            if (await _voucherRepository.CodeExistsAsync(voucherDto.Code, existingVoucher.VoucherID))
                throw new InvalidOperationException("Mã voucher đã tồn tại");
        }

        // Kiểm tra khoảng thời gian nếu có thay đổi
        var startDate = voucherDto.StartDate ?? existingVoucher.StartDate;
        var endDate = voucherDto.EndDate ?? existingVoucher.EndDate;
        if (startDate >= endDate)
            throw new InvalidOperationException("Ngày bắt đầu phải trước ngày kết thúc");

        // Kiểm tra phần trăm giảm giá nếu có thay đổi
        if (voucherDto.Percent.HasValue && (voucherDto.Percent.Value <= 0 || voucherDto.Percent.Value > 100))
            throw new InvalidOperationException("Phần trăm giảm giá phải từ 1 đến 100");

        // Kiểm tra số lần sử dụng tối đa nếu có thay đổi
        if (voucherDto.MaxUsage.HasValue)
        {
            if (voucherDto.MaxUsage.Value <= 0)
                throw new InvalidOperationException("Số lần sử dụng tối đa phải lớn hơn 0");

            // Kiểm tra không thể giảm số lần sử dụng xuống dưới số lần đã sử dụng
            var usedCount = existingVoucher.UserVouchers.Count(uv => uv.IsUsed);
            if (voucherDto.MaxUsage.Value < usedCount)
                throw new InvalidOperationException($"Không thể giảm số lần sử dụng xuống dưới số lần đã sử dụng ({usedCount})");
        }
    }
}