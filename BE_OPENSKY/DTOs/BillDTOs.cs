using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.DTOs
{
    // DTO cho Bill response
    public class BillResponseDTO
    {
        public Guid BillID { get; set; }
        public Guid UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string TableType { get; set; } = string.Empty;
        public Guid TypeID { get; set; }
        public decimal Deposit { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<BillDetailResponseDTO> BillDetails { get; set; } = new();
    }

    // DTO cho BillDetail response
    public class BillDetailResponseDTO
    {
        public Guid BillDetailID { get; set; }
        public Guid BillID { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public Guid ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
