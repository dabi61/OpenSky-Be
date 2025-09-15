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
                // Kiểm tra tour tồn tại
                var tour = await _context.Tours.FindAsync(createBookingDto.TourID);
                if (tour == null)
                    throw new ArgumentException("Tour không tồn tại");

                // Kiểm tra ngày booking hợp lệ
                if (createBookingDto.StartDate < DateTime.UtcNow)
                    throw new ArgumentException("Không thể đặt tour trong quá khứ");

                if (createBookingDto.StartDate > createBookingDto.EndDate)
                    throw new ArgumentException("Ngày bắt đầu không thể sau ngày kết thúc");

                // Kiểm tra số người hợp lệ
                if (createBookingDto.NumberOfGuests <= 0)
                    throw new ArgumentException("Số người phải lớn hơn 0");

                // Tìm schedule phù hợp với tour và thời gian
                var schedule = await _context.Schedules
                    .Where(s => s.TourID == createBookingDto.TourID 
                               && s.StartTime.Date == createBookingDto.StartDate.Date
                               && s.Status == ScheduleStatus.Active)
                    .FirstOrDefaultAsync();

                if (schedule == null)
                    throw new ArgumentException("Không tìm thấy schedule phù hợp cho tour này");

                // Kiểm tra capacity của schedule
                if (schedule.CurrentBookings + createBookingDto.NumberOfGuests > schedule.NumberPeople)
                    throw new ArgumentException($"Schedule không còn đủ chỗ. Còn lại: {schedule.NumberPeople - schedule.CurrentBookings} chỗ");

                // Cập nhật số người đã đặt trong schedule
                schedule.CurrentBookings += createBookingDto.NumberOfGuests;

                // Tạo booking
                var booking = new Booking
                {
                    BookingID = Guid.NewGuid(),
                    UserID = userId,
                    TourID = createBookingDto.TourID,
                    CheckInDate = createBookingDto.StartDate,
                    CheckOutDate = createBookingDto.EndDate,
                    Notes = createBookingDto.Notes,
                    Status = BookingStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Bookings.Add(booking);
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

        public async Task<bool> UpdateTourBookingAsync(Guid bookingId, Guid userId, UpdateTourBookingDTO updateBookingDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.UserID == userId && b.TourID != null);

                if (booking == null)
                    return false;

                // Chỉ cho phép cập nhật booking chưa bắt đầu
                if (booking.Status != BookingStatus.Pending)
                    throw new ArgumentException("Chỉ có thể cập nhật booking chưa bắt đầu");

                // Cập nhật các trường
                if (updateBookingDto.StartDate.HasValue)
                    booking.CheckInDate = updateBookingDto.StartDate.Value;

                if (updateBookingDto.EndDate.HasValue)
                    booking.CheckOutDate = updateBookingDto.EndDate.Value;

                if (updateBookingDto.Notes != null)
                    booking.Notes = updateBookingDto.Notes;

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
