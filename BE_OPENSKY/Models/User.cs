// using directives đã đưa vào GlobalUsings

namespace BE_OPENSKY.Models
{
    public class User
    {
        [Key]
        public Guid UserID { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        public string? ProviderId { get; set; }
        
        [Required]
        public string Role { get; set; } = RoleConstants.Customer; // Vai trò: Supervisor, TourGuide, Admin, Customer, Hotel
        
        public UserStatus Status { get; set; } = UserStatus.Active; // Trạng thái người dùng
        
        public string? PhoneNumber { get; set; } // Số điện thoại
        
        public string? CitizenId { get; set; } // Số CMND/CCCD
        
        public DateOnly? DoB { get; set; } // Ngày sinh
        
        public string? AvatarURL { get; set; } // Link ảnh đại diện
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Ngày tạo tài khoản
        
        // Thuộc tính điều hướng - Các mối quan hệ với bảng khác
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>(); // Phiên đăng nhập
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>(); // Tin nhắn đã gửi
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>(); // Tin nhắn đã nhận
        public virtual ICollection<FeedBack> FeedBacks { get; set; } = new List<FeedBack>(); // Đánh giá đã viết
        public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>(); // Hóa đơn
        public virtual ICollection<Tour> Tours { get; set; } = new List<Tour>(); // Tour đã tạo (nếu là TourGuide)
        public virtual ICollection<Hotel> Hotels { get; set; } = new List<Hotel>(); // Hotel đã tạo (nếu là Hotel)
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>(); // Lịch trình đã đặt
        public ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>(); // Voucher đã lưu

    }
}
