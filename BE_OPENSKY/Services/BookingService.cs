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
                Notes = null, // Notes đã được bỏ khỏi DTO
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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

        public async Task<PaginatedBookingsResponseDTO> GetBookingsPaginatedAsync(int page = 1, int limit = 10, string? status = null, Guid? userId = null, Guid? hotelId = null)
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            limit = Math.Max(1, Math.Min(100, limit));

            var query = _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Room)
                .Include(b => b.User)
                .Include(b => b.Bill)
                .AsQueryable();

            // Lọc theo user nếu có
            if (userId.HasValue)
            {
                query = query.Where(b => b.UserID == userId.Value);
            }

            // Lọc theo hotel nếu có
            if (hotelId.HasValue)
            {
                query = query.Where(b => b.HotelID == hotelId.Value);
            }

            // Lọc theo status nếu có
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
            {
                query = query.Where(b => b.Status == bookingStatus);
            }

            // Đếm tổng số bookings
            var totalBookings = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalBookings / limit);

            // Lấy bookings với phân trang
            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(b => new BookingResponseDTO
                {
                    BookingID = b.BookingID,
                    UserID = b.UserID,
                    UserName = b.User.FullName,
                    BookingType = b.BookingType,
                    HotelID = b.HotelID,
                    HotelName = b.Hotel != null ? b.Hotel.HotelName : "",
                    RoomID = b.RoomID,
                    RoomName = b.Room != null ? b.Room.RoomName : "",
                    RoomType = b.Room != null ? b.Room.RoomType : "",
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
                })
                .ToListAsync();

            return new PaginatedBookingsResponseDTO
            {
                Bookings = bookings,
                CurrentPage = page,
                PageSize = limit,
                TotalBookings = totalBookings,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<PaginatedBookingsResponseDTO> SearchBookingsAsync(BookingSearchDTO searchDto)
        {
            // Validate pagination parameters
            searchDto.Page = Math.Max(1, searchDto.Page);
            searchDto.Limit = Math.Max(1, Math.Min(100, searchDto.Limit));

            var query = _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Room)
                .Include(b => b.User)
                .Include(b => b.Bill)
                .AsQueryable();

            // Tìm kiếm theo query (tên khách, email, số điện thoại)
            if (!string.IsNullOrWhiteSpace(searchDto.Query))
            {
                var searchTerm = searchDto.Query.ToLower();
                query = query.Where(b => 
                    (b.GuestName != null && b.GuestName.ToLower().Contains(searchTerm)) ||
                    (b.GuestEmail != null && b.GuestEmail.ToLower().Contains(searchTerm)) ||
                    (b.GuestPhone != null && b.GuestPhone.Contains(searchTerm)) ||
                    (b.User.FullName != null && b.User.FullName.ToLower().Contains(searchTerm)) ||
                    (b.User.Email != null && b.User.Email.ToLower().Contains(searchTerm))
                );
            }

            // Lọc theo status
            if (!string.IsNullOrEmpty(searchDto.Status) && Enum.TryParse<BookingStatus>(searchDto.Status, true, out var bookingStatus))
            {
                query = query.Where(b => b.Status == bookingStatus);
            }

            // Lọc theo ngày check-in
            if (searchDto.FromDate.HasValue)
            {
                query = query.Where(b => b.CheckInDate >= searchDto.FromDate.Value);
            }

            // Lọc theo ngày check-out
            if (searchDto.ToDate.HasValue)
            {
                query = query.Where(b => b.CheckOutDate <= searchDto.ToDate.Value);
            }

            // Lọc theo hotel
            if (searchDto.HotelId.HasValue)
            {
                query = query.Where(b => b.HotelID == searchDto.HotelId.Value);
            }

            // Lọc theo phòng
            if (searchDto.RoomId.HasValue)
            {
                query = query.Where(b => b.RoomID == searchDto.RoomId.Value);
            }

            // Lọc theo loại booking
            if (!string.IsNullOrEmpty(searchDto.BookingType))
            {
                query = query.Where(b => b.BookingType == searchDto.BookingType);
            }

            // Sắp xếp
            query = searchDto.SortBy?.ToLower() switch
            {
                "checkindate" => searchDto.SortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(b => b.CheckInDate)
                    : query.OrderByDescending(b => b.CheckInDate),
                "totalprice" => searchDto.SortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(b => b.TotalPrice)
                    : query.OrderByDescending(b => b.TotalPrice),
                _ => searchDto.SortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(b => b.CreatedAt)
                    : query.OrderByDescending(b => b.CreatedAt)
            };

            // Đếm tổng số bookings
            var totalBookings = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalBookings / searchDto.Limit);

            // Lấy bookings với phân trang
            var bookings = await query
                .Skip((searchDto.Page - 1) * searchDto.Limit)
                .Take(searchDto.Limit)
                .Select(b => new BookingResponseDTO
                {
                    BookingID = b.BookingID,
                    UserID = b.UserID,
                    UserName = b.User.FullName,
                    BookingType = b.BookingType,
                    HotelID = b.HotelID,
                    HotelName = b.Hotel != null ? b.Hotel.HotelName : "",
                    RoomID = b.RoomID,
                    RoomName = b.Room != null ? b.Room.RoomName : "",
                    RoomType = b.Room != null ? b.Room.RoomType : "",
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
                })
                .ToListAsync();

            return new PaginatedBookingsResponseDTO
            {
                Bookings = bookings,
                CurrentPage = searchDto.Page,
                PageSize = searchDto.Limit,
                TotalBookings = totalBookings,
                TotalPages = totalPages,
                HasNextPage = searchDto.Page < totalPages,
                HasPreviousPage = searchDto.Page > 1
            };
        }

        public async Task<PaginatedHotelBookingsResponseDTO> GetHotelBookingsPaginatedAsync(Guid hotelId, Guid userId, int page = 1, int limit = 10, string? status = null)
        {
            // Kiểm tra quyền sở hữu khách sạn
            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h => h.HotelID == hotelId && h.UserID == userId);
            
            if (hotel == null)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập khách sạn này");

            // Lấy booking của hotel với phân trang
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Hotel)
                .Include(b => b.Room)
                .Where(b => b.HotelID == hotelId);

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(status))
            {
                // Parse string status to enum for comparison
                if (Enum.TryParse<BookingStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(b => b.Status == statusEnum);
                }
            }

            // Đếm tổng số booking
            var totalBookings = await query.CountAsync();

            // Tính phân trang
            var totalPages = (int)Math.Ceiling((double)totalBookings / limit);
            var skip = (page - 1) * limit;

            // Lấy booking với phân trang
            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip(skip)
                .Take(limit)
                .Select(b => new HotelBookingResponseDTO
                {
                    BookingID = b.BookingID,
                    UserID = b.UserID,
                    UserName = b.User.FullName,
                    HotelID = b.HotelID ?? Guid.Empty,
                    HotelName = b.Hotel.HotelName ?? string.Empty,
                    RoomID = b.RoomID ?? Guid.Empty,
                    RoomName = b.Room.RoomName ?? string.Empty,
                    RoomType = b.Room.RoomType ?? string.Empty,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    NumberOfGuests = 1, // Default value since NumberOfGuests doesn't exist in Booking model
                    TotalPrice = b.TotalPrice,
                    Status = b.Status.ToString(),
                    GuestName = b.GuestName ?? string.Empty,
                    GuestPhone = b.GuestPhone ?? string.Empty,
                    GuestEmail = b.GuestEmail ?? string.Empty,
                    PaymentMethod = b.PaymentMethod,
                    PaymentStatus = b.PaymentStatus,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();

            return new PaginatedHotelBookingsResponseDTO
            {
                Bookings = bookings,
                CurrentPage = page,
                PageSize = limit,
                TotalBookings = totalBookings,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<RoomAvailabilityResponseDTO> CheckRoomAvailabilityAsync(RoomAvailabilityCheckDTO checkDto)
        {
            // Kiểm tra phòng có tồn tại không
            var room = await _context.HotelRooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.RoomID == checkDto.RoomId);

            if (room == null)
            {
                return new RoomAvailabilityResponseDTO
                {
                    IsAvailable = false,
                    Message = "Không tìm thấy phòng"
                };
            }

            // Kiểm tra phòng có đang bảo trì không
            if (room.Status == RoomStatus.Maintenance)
            {
                return new RoomAvailabilityResponseDTO
                {
                    IsAvailable = false,
                    Message = "Phòng đang trong quá trình bảo trì"
                };
            }

            // Kiểm tra ngày check-in phải sau ngày hiện tại
            if (checkDto.CheckInDate <= DateTime.UtcNow.Date)
            {
                return new RoomAvailabilityResponseDTO
                {
                    IsAvailable = false,
                    Message = "Ngày check-in phải sau ngày hiện tại"
                };
            }

            // Kiểm tra ngày check-out phải sau check-in
            if (checkDto.CheckOutDate <= checkDto.CheckInDate)
            {
                return new RoomAvailabilityResponseDTO
                {
                    IsAvailable = false,
                    Message = "Ngày check-out phải sau ngày check-in"
                };
            }

            // Tìm các booking xung đột
            var conflictingBookings = await _context.Bookings
                .Where(b => b.RoomID == checkDto.RoomId &&
                           b.Status != BookingStatus.Cancelled &&
                           b.Status != BookingStatus.Refunded &&
                           ((b.CheckInDate < checkDto.CheckOutDate && b.CheckOutDate > checkDto.CheckInDate)))
                .Select(b => new BookingConflictDTO
                {
                    BookingId = b.BookingID,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status.ToString(),
                    GuestName = b.GuestName ?? b.User.FullName
                })
                .ToListAsync();

            // Tính giá
            var numberOfNights = (int)(checkDto.CheckOutDate - checkDto.CheckInDate).TotalDays;
            var totalPrice = room.Price * numberOfNights;

            if (conflictingBookings.Any())
            {
                return new RoomAvailabilityResponseDTO
                {
                    IsAvailable = false,
                    Message = $"Phòng không có sẵn trong khoảng thời gian từ {checkDto.CheckInDate:dd/MM/yyyy} đến {checkDto.CheckOutDate:dd/MM/yyyy}",
                    Conflicts = conflictingBookings,
                    Price = room.Price,
                    NumberOfNights = numberOfNights,
                    TotalPrice = totalPrice
                };
            }

            return new RoomAvailabilityResponseDTO
            {
                IsAvailable = true,
                Message = "Phòng có sẵn",
                Price = room.Price,
                NumberOfNights = numberOfNights,
                TotalPrice = totalPrice
            };
        }

        public async Task<BookingStatsDTO> GetBookingStatsAsync(Guid? hotelId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Bookings.AsQueryable();

            // Lọc theo hotel nếu có
            if (hotelId.HasValue)
            {
                query = query.Where(b => b.HotelID == hotelId.Value);
            }

            // Lọc theo ngày nếu có
            if (fromDate.HasValue)
            {
                query = query.Where(b => b.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.CreatedAt <= toDate.Value);
            }

            var bookings = await query.ToListAsync();

            var stats = new BookingStatsDTO
            {
                TotalBookings = bookings.Count,
                PendingBookings = bookings.Count(b => b.Status == BookingStatus.Pending),
                ConfirmedBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed),
                CancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled),
                CompletedBookings = bookings.Count(b => b.Status == BookingStatus.Completed),
                RefundedBookings = bookings.Count(b => b.Status == BookingStatus.Refunded),
                TotalRevenue = bookings.Sum(b => b.TotalPrice),
                PendingRevenue = bookings.Where(b => b.Status == BookingStatus.Pending).Sum(b => b.TotalPrice),
                ConfirmedRevenue = bookings.Where(b => b.Status == BookingStatus.Confirmed).Sum(b => b.TotalPrice),
                CompletedRevenue = bookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.TotalPrice)
            };

            // Thống kê theo tháng
            var monthlyStats = bookings
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .Select(g => new MonthlyStatsDTO
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Bookings = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderBy(s => s.Year)
                .ThenBy(s => s.Month)
                .ToList();

            stats.MonthlyStats = monthlyStats;

            return stats;
        }

        public async Task<bool> UpdateBookingPaymentStatusAsync(Guid billId, string paymentStatus)
        {
            try
            {
                // Tìm booking theo bill ID
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BillID == billId);

                if (booking == null)
                    return false;

                // Cập nhật trạng thái thanh toán
                booking.PaymentStatus = paymentStatus;
                booking.UpdatedAt = DateTime.UtcNow;

                // Nếu thanh toán thành công, có thể cập nhật trạng thái booking
                if (paymentStatus == "Paid" && booking.Status == BookingStatus.Confirmed)
                {
                    // Có thể thêm logic để cập nhật trạng thái booking nếu cần
                    // booking.Status = BookingStatus.Completed;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
