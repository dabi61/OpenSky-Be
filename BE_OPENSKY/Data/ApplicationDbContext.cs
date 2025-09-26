// using directives đã đưa vào GlobalUsings

namespace BE_OPENSKY.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<HotelRoom> HotelRooms { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillDetail> BillDetails { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<TourItinerary> TourItineraries { get; set; }
        public DbSet<ScheduleItinerary> ScheduleItineraries { get; set; }
        public DbSet<FeedBack> FeedBacks { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<UserVoucher> UserVouchers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.CitizenId).HasMaxLength(20);
                entity.Property(e => e.dob).HasColumnType("date"); // Cấu hình cho ngày sinh
                entity.Property(e => e.AvatarURL).HasMaxLength(500);

                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Session
            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasKey(e => e.SessionID);
                entity.Property(e => e.RefreshToken).IsRequired().HasMaxLength(500);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

                entity.HasOne(e => e.User)
                    .WithMany(e => e.Sessions)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Index để tìm session theo RefreshToken nhanh
                entity.HasIndex(e => e.RefreshToken).IsUnique();
                // Index để tìm session active của user
                entity.HasIndex(e => new { e.UserID, e.IsActive });
            });

            // Tour
            modelBuilder.Entity<Tour>(entity =>
            {
                entity.HasKey(e => e.TourID);
                entity.Property(e => e.TourName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Province).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Star).IsRequired();
                entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.MaxPeople).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany(e => e.Tours)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Hotel
            modelBuilder.Entity<Hotel>(entity =>
            {
                entity.HasKey(e => e.HotelID);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Province).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Latitude).HasColumnType("decimal(18,15)").IsRequired();
                entity.Property(e => e.Longitude).HasColumnType("decimal(18,15)").IsRequired();
                entity.Property(e => e.HotelName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();
                entity.Property(e => e.Star).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany(e => e.Hotels)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // HotelRoom
            modelBuilder.Entity<HotelRoom>(entity =>
            {
                entity.HasKey(e => e.RoomID);
                entity.Property(e => e.RoomName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RoomType).IsRequired().HasMaxLength(50); // String type
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).IsRequired().HasConversion<string>(); // RoomStatus enum

                entity.HasOne(e => e.Hotel)
                    .WithMany(e => e.HotelRooms)
                    .HasForeignKey(e => e.HotelID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Message
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.MessageID);
                entity.Property(e => e.MessageText).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.IsReaded).IsRequired().HasDefaultValue(false);
                entity.HasOne(e => e.SenderUser)
                    .WithMany(e => e.SentMessages)
                    .HasForeignKey(e => e.Sender)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ReceiverUser)
                    .WithMany(e => e.ReceivedMessages)
                    .HasForeignKey(e => e.Receiver)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Bill
            modelBuilder.Entity<Bill>(entity =>
            {
                entity.HasKey(e => e.BillID);
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();
                entity.Property(e => e.Deposit).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.RefundPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany(e => e.Bills)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UserVoucher)
                    .WithMany()
                    .HasForeignKey(e => e.UserVoucherID)
                    .OnDelete(DeleteBehavior.SetNull);

                // One-to-one with Booking via BookingID
                entity.HasOne(e => e.Booking)
                    .WithOne(b => b.Bill)
                    .HasForeignKey<Bill>(e => e.BookingID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.BookingID).IsUnique().HasFilter("\"BookingID\" IS NOT NULL");
            });

            // BillDetail
            modelBuilder.Entity<BillDetail>(entity =>
            {
                entity.HasKey(e => e.BillDetailID);
                entity.Property(e => e.ItemType).IsRequired().HasConversion<string>();
                entity.Property(e => e.ItemName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.Bill)
                    .WithMany(e => e.BillDetails)
                    .HasForeignKey(e => e.BillID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.RoomID, e.ScheduleID });
            });

            // Schedule
            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.HasKey(e => e.ScheduleID);
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();
                entity.Property(e => e.NumberPeople).IsRequired();

                entity.HasOne(e => e.Tour)
                    .WithMany(e => e.Schedules)
                    .HasForeignKey(e => e.TourID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany(e => e.Schedules)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // ScheduleItinerary
            modelBuilder.Entity<ScheduleItinerary>(entity =>
            {
                entity.HasKey(e => e.ScheduleItID);
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();

                entity.HasOne(e => e.Schedule)
                    .WithMany(e => e.ScheduleItineraries)
                    .HasForeignKey(e => e.ScheduleID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.TourItinerary)
                    .WithMany(e => e.ScheduleItineraries)
                    .HasForeignKey(e => e.ItineraryID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // FeedBack
            modelBuilder.Entity<FeedBack>(entity =>
            {
                entity.HasKey(e => e.FeedBackID);
                entity.Property(e => e.TableType).IsRequired().HasConversion<string>();
                entity.Property(e => e.Rate).IsRequired().HasMaxLength(1);
                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.HasOne(e => e.User)
                    .WithMany(e => e.FeedBacks)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Refund
            modelBuilder.Entity<Refund>(entity =>
            {
                entity.HasKey(e => e.RefundID);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();

                entity.HasOne(e => e.Bill)
                    .WithMany(e => e.Refunds)
                    .HasForeignKey(e => e.BillID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationID);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);

                entity.HasOne(e => e.Bill)
                    .WithMany(e => e.Notifications)
                    .HasForeignKey(e => e.BillID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Image - Cấu hình bảng quản lý ảnh
            modelBuilder.Entity<Image>(entity =>
            {
                entity.HasKey(e => e.ImgID); // Khóa chính
                entity.Property(e => e.TableType).IsRequired().HasConversion<string>(); // Loại đối tượng (bắt buộc)
                entity.Property(e => e.TypeID).IsRequired(); // ID đối tượng (bắt buộc)
                entity.Property(e => e.URL).IsRequired().HasMaxLength(500); // Link ảnh (bắt buộc)
                entity.Property(e => e.CreatedAt).IsRequired(); // Ngày tạo (bắt buộc)
                
                // Index để truy vấn nhanh theo đối tượng
                entity.HasIndex(e => new { e.TableType, e.TypeID })
                    .HasDatabaseName("IX_Images_TableType_TypeID");
            });

            // Voucher - Cấu hình bảng mã giảm giá
            modelBuilder.Entity<Voucher>(entity =>
            {
                entity.HasKey(e => e.VoucherID); // Khóa chính
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50); // Mã voucher (bắt buộc)
                entity.HasIndex(e => e.Code).IsUnique(); // Mã voucher phải duy nhất
                entity.Property(e => e.Percent).IsRequired(); // Phần trăm giảm giá (bắt buộc)
                entity.Property(e => e.TableType).IsRequired().HasConversion<string>(); // Loại voucher (bắt buộc)
                entity.Property(e => e.StartDate).IsRequired(); // Ngày bắt đầu (bắt buộc)
                entity.Property(e => e.EndDate).IsRequired(); // Ngày hết hạn (bắt buộc)
                entity.Property(e => e.Description).HasMaxLength(1000); // Mô tả voucher
                entity.Property(e => e.MaxUsage).IsRequired(); // Số lần sử dụng tối đa (bắt buộc)
            });

            // UserVoucher - Cấu hình bảng voucher đã lưu của khách hàng
            modelBuilder.Entity<UserVoucher>(entity =>
            {
                entity.HasKey(e => e.UserVoucherID); // Khóa chính
                entity.Property(e => e.IsUsed).IsRequired(); // Trạng thái sử dụng (bắt buộc)
                entity.Property(e => e.SavedAt).IsRequired(); // Ngày lưu voucher (bắt buộc)

                // Quan hệ với bảng User
                entity.HasOne(e => e.User)
                    .WithMany(e => e.UserVouchers)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade); // Xóa user thì xóa luôn voucher đã lưu

                // Quan hệ với bảng Voucher
                entity.HasOne(e => e.Voucher)
                    .WithMany(e => e.UserVouchers)
                    .HasForeignKey(e => e.VoucherID)
                    .OnDelete(DeleteBehavior.Cascade); // Xóa voucher thì xóa luôn bản ghi đã lưu
            });

            // Booking
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.BookingID);
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.PaymentStatus).HasMaxLength(100);

                // Quan hệ với bảng User
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ với bảng Hotel (optional)
                entity.HasOne(e => e.Hotel)
                    .WithMany()
                    .HasForeignKey(e => e.HotelID)
                    .OnDelete(DeleteBehavior.SetNull);

                // One-to-one Booking - Bill is configured in Bill entity

                // Indexes for performance
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Schedule
            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.HasKey(e => e.ScheduleID);
                entity.Property(e => e.Status).IsRequired().HasConversion<string>();
                entity.Property(e => e.NumberPeople).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                // Indexes for performance
                entity.HasIndex(e => e.TourID);
                entity.HasIndex(e => e.UserID);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
            });

            // TourItinerary
            modelBuilder.Entity<TourItinerary>(entity =>
            {
                entity.HasKey(e => e.ItineraryID);
                entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DayNumber).IsRequired();
                entity.Property(e => e.IsDeleted).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.Tour)
                    .WithMany(e => e.TourItineraries)
                    .HasForeignKey(e => e.TourID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes for performance
                entity.HasIndex(e => e.TourID);
                entity.HasIndex(e => e.DayNumber);
                entity.HasIndex(e => e.IsDeleted);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ScheduleItinerary
            modelBuilder.Entity<ScheduleItinerary>(entity =>
            {
                entity.HasKey(e => e.ScheduleItID);

                // Quan hệ với bảng Schedule
                entity.HasOne(e => e.Schedule)
                    .WithMany(s => s.ScheduleItineraries)
                    .HasForeignKey(e => e.ScheduleID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Quan hệ với bảng TourItinerary
                entity.HasOne(e => e.TourItinerary)
                    .WithMany(ti => ti.ScheduleItineraries)
                    .HasForeignKey(e => e.ItineraryID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes for performance
                entity.HasIndex(e => e.ScheduleID);
                entity.HasIndex(e => e.ItineraryID);
            });
        }
    }
}
