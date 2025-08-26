// using directives đã đưa vào GlobalUsings

namespace BE_OPENSKY.Models
{
    public class Message
    {
        [Key]
        public int MessageID { get; set; }
        
        [Required]
        public int Sender { get; set; }
        
        [Required]
        public int Receiver { get; set; }
        
        [Required]
        public string MessageText { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Thuộc tính điều hướng
        public virtual User SenderUser { get; set; } = null!; // Người gửi
        public virtual User ReceiverUser { get; set; } = null!; // Người nhận
    }
}
