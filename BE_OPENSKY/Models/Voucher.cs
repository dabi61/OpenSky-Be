namespace BE_OPENSKY.Models
{
    public class Voucher
    {
        public Guid VoucherID { get; set; }  // UUID
        public string Code { get; set; }     // Unique code
        public int Percent { get; set; }
        public string TableType { get; set; }  // "Tour" hoặc "Hotel"
        public Guid TableID { get; set; }      // ID của Hotel hoặc Tour
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }

        // Navigation property
        public ICollection<UserVoucher> UserVouchers { get; set; }
    }
}
