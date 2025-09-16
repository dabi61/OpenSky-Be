using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BE_OPENSKY.Services
{
    public class TourBookingService : ITourBookingService
    {
        private readonly ApplicationDbContext _context;

        public TourBookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateTourBookingAsync(Guid userId, CreateTourBookingDTO createBookingDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lấy schedule và tour
                var schedule = await _context.Schedules
                    .Include(s => s.Tour)
                    .FirstOrDefaultAsync(s => s.ScheduleID == createBookingDto.ScheduleID && s.Status == ScheduleStatus.Active);
                if (schedule == null)
                    throw new ArgumentException("Schedule không tồn tại hoặc không khả dụng");

                var tour = schedule.Tour;
                if (tour == null)
                    throw new ArgumentException("Không tìm thấy tour cho schedule");

                // TourID được lấy trực tiếp từ Schedule; không nhận từ request

                // Kiểm tra số người hợp lệ và capacity
                if (createBookingDto.NumberOfGuests <= 0)
                    throw new ArgumentException("Số người phải lớn hơn 0");

                if (schedule.CurrentBookings + createBookingDto.NumberOfGuests > schedule.NumberPeople)
                    throw new ArgumentException($"Schedule không còn đủ chỗ. Còn lại: {schedule.NumberPeople - schedule.CurrentBookings} chỗ");

                // KHÔNG cộng capacity ở bước đặt. Chỉ cộng sau khi thanh toán thành công.

                // Tạo booking với ngày theo schedule
                var booking = new Booking
                {
                    BookingID = Guid.NewGuid(),
                    UserID = userId,
                    TourID = schedule.TourID,
                    CheckInDate = schedule.StartTime,
                    CheckOutDate = schedule.EndTime,
                    Notes = createBookingDto.Notes,
                    Status = BookingStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Tạo Bill Pending cho booking
                var unitPrice = tour.Price;
                var quantity = createBookingDto.NumberOfGuests;
                var totalPrice = unitPrice * quantity;

                var bill = new Bill
                {
                    BillID = Guid.NewGuid(),
                    UserID = userId,
                    BookingID = booking.BookingID,
                    Deposit = 0,
                    TotalPrice = totalPrice,
                    Status = BillStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var billDetail = new BillDetail
                {
                    BillDetailID = Guid.NewGuid(),
                    BillID = bill.BillID,
                    ItemType = TableType.Tour,
                    ItemID = tour.TourID,
                    ScheduleID = schedule.ScheduleID,
                    ItemName = tour.TourName,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    Notes = $"Tour booking cho schedule {schedule.ScheduleID}"
                };

                bill.BillDetails.Add(billDetail);
                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return booking.BookingID;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<TourBookingResponseDTO?> GetTourBookingByIdAsync(Guid bookingId, Guid userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Tour)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.UserID == userId && b.TourID != null);

            if (booking == null)
                return null;

            return await MapToTourBookingResponseDTO(booking);
        }

        public async Task<TourBookingListResponseDTO> GetUserTourBookingsAsync(Guid userId, int page = 1, int size = 10)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Tour)
                .Where(b => b.UserID == userId && b.TourID != null)
                .OrderByDescending(b => b.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var bookings = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var bookingDTOs = new List<TourBookingResponseDTO>();
            foreach (var booking in bookings)
            {
                bookingDTOs.Add(await MapToTourBookingResponseDTO(booking));
            }

            return new TourBookingListResponseDTO
            {
                Bookings = bookingDTOs,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        public async Task<TourBookingListResponseDTO> GetAllTourBookingsAsync(int page = 1, int size = 10)
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Tour)
                .Where(b => b.TourID != null)
                .OrderByDescending(b => b.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var bookings = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var bookingDTOs = new List<TourBookingResponseDTO>();
            foreach (var booking in bookings)
            {
                bookingDTOs.Add(await MapToTourBookingResponseDTO(booking));
            }

            return new TourBookingListResponseDTO
            {
                Bookings = bookingDTOs,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        

        public async Task<bool> CancelTourBookingAsync(Guid bookingId, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.UserID == userId && b.TourID != null);

                if (booking == null)
                    return false;

                // Chỉ cho phép hủy booking chưa bắt đầu
                if (booking.Status != BookingStatus.Pending)
                    throw new ArgumentException("Chỉ có thể hủy booking chưa bắt đầu");

                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<TourBookingResponseDTO> MapToTourBookingResponseDTO(Booking booking)
        {
            // Lấy số người từ BillDetail (Schedule)
            var billDetail = await _context.BillDetails
                .FirstOrDefaultAsync(bd => bd.BillID == booking.BookingID && bd.ItemType == TableType.Schedule);
            
            var numberOfGuests = billDetail?.Quantity ?? 0;

            return new TourBookingResponseDTO
            {
                BookingID = booking.BookingID,
                UserID = booking.UserID,
                UserName = booking.User.FullName,
                TourID = booking.TourID ?? Guid.Empty,
                TourName = booking.Tour?.TourName ?? "",
                StartDate = booking.CheckInDate,
                EndDate = booking.CheckOutDate,
                NumberOfGuests = numberOfGuests,
                Status = booking.Status.ToString(),
                Notes = booking.Notes,
                PaymentMethod = booking.PaymentMethod,
                PaymentStatus = booking.PaymentStatus,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                TourInfo = booking.Tour != null ? new TourInfoDTO
                {
                    TourID = booking.Tour.TourID,
                    TourName = booking.Tour.TourName,
                    Description = booking.Tour.Description,
                    Price = booking.Tour.Price,
                    Duration = $"{booking.Tour.MaxPeople} người", // Sử dụng MaxPeople thay vì Duration
                    Location = $"{booking.Tour.Address}, {booking.Tour.Province}" // Kết hợp Address và Province
                } : null
            };
        }
    }
}
