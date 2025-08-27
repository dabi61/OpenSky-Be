namespace BE_OPENSKY.Models
{
    // Model UserVoucher - Voucher đã lưu của khách hàng
    public class UserVoucher
    {
        public Guid UserVoucherID { get; set; } // ID bản ghi voucher của user
        public Guid UserID { get; set; }         // ID khách hàng
        public Guid VoucherID { get; set; }     // ID voucher
        public bool IsUsed { get; set; }        // Đã sử dụng chưa
        public DateTime SavedAt { get; set; }   // Ngày lưu voucher

        // Thuộc tính điều hướng
        public User User { get; set; }          // Thông tin khách hàng
        public Voucher Voucher { get; set; }    // Thông tin voucher
    }
}
