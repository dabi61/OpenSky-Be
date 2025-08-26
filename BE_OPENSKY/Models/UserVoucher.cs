namespace BE_OPENSKY.Models
{
    public class UserVoucher
    {
        public Guid UserVoucherID { get; set; }
        public int UserID { get; set; }
        public Guid VoucherID { get; set; }
        public bool IsUsed { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Voucher Voucher { get; set; }
    }
}
