using Microsoft.EntityFrameworkCore;
using BE_OPENSKY.Models;

namespace BE_OPENSKY.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<HotelRoom> HotelRooms { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<TourItinerary> TourItineraries { get; set; }
        public DbSet<ScheduleItinerary> ScheduleItineraries { get; set; }
        public DbSet<FeedBack> FeedBacks { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Image> Images { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PassWord).IsRequired().HasMaxLength(100);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.NumberPhone).HasMaxLength(20);
                entity.Property(e => e.CitizenId).HasMaxLength(20);
                entity.Property(e => e.AvatarURL).HasMaxLength(500);
                
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Tour configuration
            modelBuilder.Entity<Tour>(entity =>
            {
                entity.HasKey(e => e.TourID);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Star).IsRequired();
                
                entity.HasOne(e => e.User)
                    .WithMany(e => e.Tours)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Hotel configuration
            modelBuilder.Entity<Hotel>(entity =>
            {
                entity.HasKey(e => e.HotelID);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.District).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Coordinates).HasMaxLength(100);
                entity.Property(e => e.HotelName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Star).IsRequired();
                
                entity.HasOne(e => e.User)
                    .WithMany(e => e.Hotels)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // HotelRoom configuration
            modelBuilder.Entity<HotelRoom>(entity =>
            {
                entity.HasKey(e => e.RoomID);
                entity.Property(e => e.RoomName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RoomType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
                
                entity.HasOne(e => e.Hotel)
                    .WithMany(e => e.HotelRooms)
                    .HasForeignKey(e => e.HotelID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Message configuration
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.MessageID);
                entity.Property(e => e.MessageText).IsRequired().HasMaxLength(1000);
                
                entity.HasOne(e => e.SenderUser)
                    .WithMany(e => e.SentMessages)
                    .HasForeignKey(e => e.Sender)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.ReceiverUser)
                    .WithMany(e => e.ReceivedMessages)
                    .HasForeignKey(e => e.Receiver)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Bill configuration
            modelBuilder.Entity<Bill>(entity =>
            {
                entity.HasKey(e => e.BillID);
                entity.Property(e => e.TableType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Deposit).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.RefundPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).IsRequired().HasColumnType("decimal(18,2)");
                
                entity.HasOne(e => e.User)
                    .WithMany(e => e.Bills)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Schedule configuration
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

            // TourItinerary configuration
            modelBuilder.Entity<TourItinerary>(entity =>
            {
                entity.HasKey(e => e.ItineraryID);
                entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.DayNumber).IsRequired();
                
                entity.HasOne(e => e.Tour)
                    .WithMany(e => e.TourItineraries)
                    .HasForeignKey(e => e.TourID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ScheduleItinerary configuration
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

            // FeedBack configuration
            modelBuilder.Entity<FeedBack>(entity =>
            {
                entity.HasKey(e => e.FeedBackID);
                entity.Property(e => e.TableType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Rate).IsRequired().HasMaxLength(1);
                entity.Property(e => e.Description).HasMaxLength(1000);
                
                entity.HasOne(e => e.User)
                    .WithMany(e => e.FeedBacks)
                    .HasForeignKey(e => e.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Refund configuration
            modelBuilder.Entity<Refund>(entity =>
            {
                entity.HasKey(e => e.RefundID);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                
                entity.HasOne(e => e.Bill)
                    .WithMany(e => e.Refunds)
                    .HasForeignKey(e => e.BillID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Notification configuration
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationID);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                
                entity.HasOne(e => e.Bill)
                    .WithMany(e => e.Notifications)
                    .HasForeignKey(e => e.BillID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Image configuration
            modelBuilder.Entity<Image>(entity =>
            {
                entity.HasKey(e => e.ImgID);
                entity.Property(e => e.TableType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.URL).IsRequired().HasMaxLength(500);
            });
        }
    }
}
