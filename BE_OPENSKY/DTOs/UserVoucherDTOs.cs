using BE_OPENSKY.Models;

namespace BE_OPENSKY.DTOs
{
    // DTO cho lưu voucher
    public class SaveVoucherDTO
    {
        public Guid VoucherID { get; set; }
    }

    // DTO cho response user voucher
    public class UserVoucherResponseDTO
    {
        public Guid UserVoucherID { get; set; }
        public Guid UserID { get; set; }
        public Guid VoucherID { get; set; }
        public bool IsUsed { get; set; }
        public DateTime SavedAt { get; set; }
        public string? UserName { get; set; }
        public string? VoucherCode { get; set; }
        public int? VoucherPercent { get; set; }
        public TableType? VoucherTableType { get; set; }
        public DateTime? VoucherStartDate { get; set; }
        public DateTime? VoucherEndDate { get; set; }
        public string? VoucherDescription { get; set; }
        public bool? VoucherIsExpired { get; set; }
    }

    // DTO cho danh sách user voucher có phân trang
    public class UserVoucherListResponseDTO
    {
        public List<UserVoucherResponseDTO> UserVouchers { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public int TotalPages { get; set; }
    }
}
