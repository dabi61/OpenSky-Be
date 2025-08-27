namespace BE_OPENSKY.DTOs;

// DTO tạo voucher mới (chỉ Admin)
public record VoucherCreateDTO
{
    public string Code { get; init; } = string.Empty; // Mã voucher
    public int Percent { get; init; } // Phần trăm giảm giá
    public TableType TableType { get; init; } // Loại: Tour hoặc Hotel
    public Guid TableID { get; init; } // ID của Tour hoặc Hotel
    public DateTime StartDate { get; init; } // Ngày bắt đầu hiệu lực
    public DateTime EndDate { get; init; } // Ngày hết hạn
    public string? Description { get; init; } // Mô tả voucher
    public int MaxUsage { get; init; } // Số lần sử dụng tối đa (thay vì Quantity)
}

// DTO cập nhật voucher (chỉ Admin)
public record VoucherUpdateDTO
{
    public string? Code { get; init; } // Mã voucher
    public int? Percent { get; init; } // Phần trăm giảm giá
    public DateTime? StartDate { get; init; } // Ngày bắt đầu
    public DateTime? EndDate { get; init; } // Ngày hết hạn
    public string? Description { get; init; } // Mô tả
    public int? MaxUsage { get; init; } // Số lần sử dụng tối đa
}

// DTO hiển thị thông tin voucher
public record VoucherResponseDTO
{
    public Guid VoucherID { get; init; } // ID voucher
    public string Code { get; init; } = string.Empty; // Mã voucher
    public int Percent { get; init; } // Phần trăm giảm giá
    public TableType TableType { get; init; } // Loại voucher
    public Guid TableID { get; init; } // ID Tour/Hotel
    public DateTime StartDate { get; init; } // Ngày bắt đầu
    public DateTime EndDate { get; init; } // Ngày hết hạn
    public string? Description { get; init; } // Mô tả
    public int MaxUsage { get; init; } // Số lần sử dụng tối đa
    public int UsedCount { get; init; } // Số lần đã sử dụng
    public int RemainingUsage { get; init; } // Số lần còn lại
    public bool IsActive { get; init; } // Còn hiệu lực không
    public string? RelatedItemName { get; init; } // Tên Tour/Hotel
    public DateTime CreatedAt { get; init; } // Ngày tạo
}

// DTO lưu voucher của khách hàng
public record SaveVoucherDTO
{
    public string Code { get; init; } = string.Empty; // Mã voucher khách hàng nhập
}

// DTO voucher đã lưu của khách hàng
public record UserVoucherResponseDTO
{
    public Guid UserVoucherID { get; init; } // ID bản ghi user-voucher
    public Guid UserID { get; init; } // ID khách hàng
    public string UserFullName { get; init; } = string.Empty; // Tên khách hàng
    public string UserEmail { get; init; } = string.Empty; // Email khách hàng
    public Guid VoucherID { get; init; } // ID voucher
    public string VoucherCode { get; init; } = string.Empty; // Mã voucher
    public int VoucherPercent { get; init; } // Phần trăm giảm giá
    public string VoucherDescription { get; init; } = string.Empty; // Mô tả voucher
    public bool IsUsed { get; init; } // Đã sử dụng chưa
    public DateTime SavedAt { get; init; } // Ngày lưu voucher
    public DateTime VoucherEndDate { get; init; } // Ngày hết hạn voucher
    public bool IsExpired { get; init; } // Đã hết hạn chưa
}

// DTO thống kê voucher (Admin)
public record VoucherStatisticsDTO
{
    public int TotalVouchers { get; init; } // Tổng số voucher
    public int ActiveVouchers { get; init; } // Voucher đang hiệu lực
    public int ExpiredVouchers { get; init; } // Voucher hết hạn
    public int TourVouchers { get; init; } // Voucher tour
    public int HotelVouchers { get; init; } // Voucher hotel
    public int TotalSaved { get; init; } // Tổng lượt lưu
    public int TotalUsed { get; init; } // Tổng lượt sử dụng
}
