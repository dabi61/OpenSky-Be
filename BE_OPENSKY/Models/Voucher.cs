namespace BE_OPENSKY.Models
{
    // Model Voucher - Mã giảm giá
    public class Voucher
    {
        public Guid VoucherID { get; set; }  // ID voucher (UUID)
        public string Code { get; set; }     // Mã voucher (duy nhất)
        public int Percent { get; set; }     // Phần trăm giảm giá
        public string TableType { get; set; }  // Loại: "Tour" hoặc "Hotel"
        public int TableID { get; set; }       // ID của Tour hoặc Hotel (int)
        public DateTime StartDate { get; set; } // Ngày bắt đầu hiệu lực
        public DateTime EndDate { get; set; }   // Ngày hết hạn
        public string? Description { get; set; } // Mô tả voucher
        public int MaxUsage { get; set; }       // Số lần sử dụng tối đa

        // Thuộc tính điều hướng - Danh sách khách hàng đã lưu voucher này
        public ICollection<UserVoucher> UserVouchers { get; set; }
    }
}
