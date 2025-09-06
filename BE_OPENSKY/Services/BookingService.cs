using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateHotelBookingAsync(Guid userId, CreateHotelBookingDTO createBookingDto)
        {
            // Kiểm tra phòng có tồn tại và available không
            var room = await _context.HotelRooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.RoomID == createBookingDto.RoomID);

            if (room == null)
                throw new InvalidOperationException("Không tìm thấy phòng");

            if (room.Status != RoomStatus.Available)
                throw new InvalidOperationException("Phòng không có sẵn để đặt");

            // Kiểm tra ngày check-in phải sau ngày hiện tại
            if (createBookingDto.CheckInDate <= DateTime.UtcNow)
                throw new InvalidOperationException("Ngày check-in phải sau ngày hiện tại");

            // Kiểm tra ngày check-out phải sau check-in
            if (createBookingDto.CheckOutDate <= createBookingDto.CheckInDate)
                throw new InvalidOperationException("Ngày check-out phải sau ngày check-in");

            // Tính tổng giá (giá phòng × số đêm)
            var numberOfNights = (int)(createBookingDto.CheckOutDate - createBookingDto.CheckInDate).TotalDays;
            var totalPrice = room.Price * numberOfNights;

            // Tạo booking
            var booking = new Booking
            {
                BookingID = Guid.NewGuid(),
                UserID = userId,
                BookingType = "Hotel",
                HotelID = room.HotelID,
                RoomID = createBookingDto.RoomID,
                CheckInDate = createBookingDto.CheckInDate,
                CheckOutDate = createBookingDto.CheckOutDate,
                TotalPrice = totalPrice,
                Status = BookingStatus.Pending,
                Notes = createBookingDto.Notes,
                GuestName = createBookingDto.GuestName,
                GuestPhone = createBookingDto.GuestPhone,
                GuestEmail = createBookingDto.GuestEmail,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return booking.BookingID;
        }

        public async Task<BookingListDTO> GetMyBookingsAsync(Guid userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Room)
                .Include(b => b.User)
                .Include(b => b.Bill)
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var bookingResponses = bookings.Select(b => new BookingResponseDTO
            {
                BookingID = b.BookingID,
                UserID = b.UserID,
                UserName = b.User.FullName,
                BookingType = b.BookingType,
                HotelID = b.HotelID,
                HotelName = b.Hotel?.HotelName ?? "",
                RoomID = b.RoomID,
                RoomName = b.Room?.RoomName ?? "",
                RoomType = b.Room?.RoomType ?? "",
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                Notes = b.Notes,
                GuestName = b.GuestName,
                GuestPhone = b.GuestPhone,
                GuestEmail = b.GuestEmail,
                PaymentMethod = b.PaymentMethod,
                PaymentStatus = b.PaymentStatus,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).ToList();

            var allBookings = await _context.Bookings
                .Where(b => b.UserID == userId)
                .ToListAsync();

            return new BookingListDTO
            {
                Bookings = bookingResponses,
                TotalBookings = allBookings.Count,
                PendingBookings = allBookings.Count(b => b.Status == BookingStatus.Pending),
                ConfirmedBookings = allBookings.Count(b => b.Status == BookingStatus.Confirmed),
                CancelledBookings = allBookings.Count(b => b.Status == BookingStatus.Cancelled),
                CompletedBookings = allBookings.Count(b => b.Status == BookingStatus.Completed),
                RefundedBookings = allBookings.Count(b => b.Status == BookingStatus.Refunded)
            };
        }

        public async Task<BookingListDTO> GetHotelBookingsAsync(Guid hotelId, Guid userId)
        {
            // Kiểm tra user có phải chủ khách sạn không
            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h => h.HotelID == hotelId && h.UserID == userId);

            if (hotel == null)
                throw new UnauthorizedAccessException("Bạn không có quyền xem booking của khách sạn này");

            var bookings = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Room)
                .Include(b => b.User)
                .Include(b => b.Bill)
                .Where(b => b.HotelID == hotelId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var bookingResponses = bookings.Select(b => new BookingResponseDTO
            {
                BookingID = b.BookingID,
                UserID = b.UserID,
                UserName = b.User.FullName,
                BookingType = b.BookingType,
                HotelID = b.HotelID,
                HotelName = b.Hotel?.HotelName ?? "",
                RoomID = b.RoomID,
                RoomName = b.Room?.RoomName ?? "",
                RoomType = b.Room?.RoomType ?? "",
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                Notes = b.Notes,
                GuestName = b.GuestName,
                GuestPhone = b.GuestPhone,
                GuestEmail = b.GuestEmail,
                PaymentMethod = b.PaymentMethod,
                PaymentStatus = b.PaymentStatus,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).ToList();

            var allBookings = await _context.Bookings
                .Where(b => b.HotelID == hotelId)
                .ToListAsync();

            return new BookingListDTO
            {
                Bookings = bookingResponses,
                TotalBookings = allBookings.Count,
                PendingBookings = allBookings.Count(b => b.Status == BookingStatus.Pending),
                ConfirmedBookings = allBookings.Count(b => b.Status == BookingStatus.Confirmed),
                CancelledBookings = allBookings.Count(b => b.Status == BookingStatus.Cancelled),
                CompletedBookings = allBookings.Count(b => b.Status == BookingStatus.Completed),
                RefundedBookings = allBookings.Count(b => b.Status == BookingStatus.Refunded)
            };
        }

        public async Task<bool> ConfirmBookingAsync(Guid bookingId, Guid userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Hotel)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
                return false;

            // Kiểm tra quyền sở hữu khách sạn
            if (booking.Hotel?.UserID != userId)
                return false;

            // Kiểm tra booking đang ở trạng thái Pending
            if (booking.Status != BookingStatus.Pending)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Cập nhật trạng thái booking
                booking.Status = BookingStatus.Confirmed;
                booking.UpdatedAt = DateTime.UtcNow;

                // Cập nhật trạng thái phòng
                if (booking.Room != null)
                {
                    booking.Room.Status = RoomStatus.Occupied;
                }

                // Tạo Bill
                var bill = new Bill
                {
                    BillID = Guid.NewGuid(),
                    UserID = booking.UserID,
                    TableType = TableType.Hotel,
                    TypeID = booking.HotelID ?? Guid.Empty,
                    Deposit = 0, // Có thể tính deposit sau
                    TotalPrice = booking.TotalPrice,
                    Status = BillStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Bills.Add(bill);

                // Tạo BillDetail
                var billDetail = new BillDetail
                {
                    BillDetailID = Guid.NewGuid(),
                    BillID = bill.BillID,
                    ItemType = TableType.Hotel,
                    ItemID = booking.RoomID ?? Guid.Empty,
                    ItemName = booking.Room?.RoomName ?? "Phòng khách sạn",
                    Quantity = (int)(booking.CheckOutDate - booking.CheckInDate).TotalDays,
                    UnitPrice = booking.Room?.Price ?? 0,
                    TotalPrice = booking.TotalPrice,
                    Notes = $"Booking phòng từ {booking.CheckInDate:dd/MM/yyyy} đến {booking.CheckOutDate:dd/MM/yyyy}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.BillDetails.Add(billDetail);

                // Liên kết booking với bill
                booking.BillID = bill.BillID;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, string? reason = null)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Hotel)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
                return false;

            // Kiểm tra quyền sở hữu khách sạn
            if (booking.Hotel?.UserID != userId)
                return false;

            // Kiểm tra booking có thể hủy không
            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Cập nhật trạng thái booking
                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(reason))
                {
                    booking.Notes = $"Hủy bởi khách sạn: {reason}";
                }

                // Cập nhật trạng thái phòng
                if (booking.Room != null)
                {
                    booking.Room.Status = RoomStatus.Available;
                }

                // Cập nhật Bill status nếu có
                if (booking.BillID.HasValue)
                {
                    var bill = await _context.Bills.FindAsync(booking.BillID);
                    if (bill != null)
                    {
                        bill.Status = BillStatus.Cancelled;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> CustomerCancelBookingAsync(Guid bookingId, Guid userId, string? reason = null)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.UserID == userId);

            if (booking == null)
                return false;

            // Kiểm tra booking có thể hủy không (chỉ được hủy khi Pending)
            if (booking.Status != BookingStatus.Pending)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Cập nhật trạng thái booking
                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(reason))
                {
                    booking.Notes = $"Hủy bởi khách hàng: {reason}";
                }

                // Cập nhật trạng thái phòng
                if (booking.Room != null)
                {
                    booking.Room.Status = RoomStatus.Available;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<BookingResponseDTO?> GetBookingByIdAsync(Guid bookingId, Guid userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Room)
                .Include(b => b.User)
                .Include(b => b.Bill)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.UserID == userId);

            if (booking == null)
                return null;

            return new BookingResponseDTO
            {
                BookingID = booking.BookingID,
                UserID = booking.UserID,
                UserName = booking.User.FullName,
                BookingType = booking.BookingType,
                HotelID = booking.HotelID,
                HotelName = booking.Hotel?.HotelName ?? "",
                RoomID = booking.RoomID,
                RoomName = booking.Room?.RoomName ?? "",
                RoomType = booking.Room?.RoomType ?? "",
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status.ToString(),
                Notes = booking.Notes,
                GuestName = booking.GuestName,
                GuestPhone = booking.GuestPhone,
                GuestEmail = booking.GuestEmail,
                PaymentMethod = booking.PaymentMethod,
                PaymentStatus = booking.PaymentStatus,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt
            };
        }

        public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, Guid userId, UpdateBookingStatusDTO updateDto)
        {
            var booking = await _context.Bookings
                .Include(b => b.Hotel)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null)
                return false;

            // Kiểm tra quyền sở hữu khách sạn
            if (booking.Hotel?.UserID != userId)
                return false;

            // Convert string status to enum
            if (!Enum.TryParse<BookingStatus>(updateDto.Status, true, out var bookingStatus))
            {
                throw new ArgumentException($"Trạng thái không hợp lệ: {updateDto.Status}");
            }

            // Cập nhật trạng thái booking
            booking.Status = bookingStatus;
            booking.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(updateDto.Notes))
            {
                booking.Notes = updateDto.Notes;
            }
            if (!string.IsNullOrEmpty(updateDto.PaymentMethod))
            {
                booking.PaymentMethod = updateDto.PaymentMethod;
            }
            if (!string.IsNullOrEmpty(updateDto.PaymentStatus))
            {
                booking.PaymentStatus = updateDto.PaymentStatus;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
