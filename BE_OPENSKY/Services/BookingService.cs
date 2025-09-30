namespace BE_OPENSKY.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;

        public BookingService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<BookingCreateResult> CreateMultipleRoomBookingAsync(Guid userId, CreateMultipleRoomBookingDTO createBookingDto)
        {
            // Validate input
            if (createBookingDto.Rooms == null || !createBookingDto.Rooms.Any())
                throw new InvalidOperationException("Phải chọn ít nhất 1 phòng");

            // Kiểm tra ngày check-in phải sau ngày hiện tại
            if (createBookingDto.CheckInDate <= DateTime.UtcNow.Date)
                throw new InvalidOperationException("Ngày check-in phải sau ngày hiện tại");

            // Kiểm tra ngày check-out phải sau check-in
            if (createBookingDto.CheckOutDate <= createBookingDto.CheckInDate)
                throw new InvalidOperationException("Ngày check-out phải sau ngày check-in");

            // Tính số đêm
            var numberOfNights = (int)(createBookingDto.CheckOutDate - createBookingDto.CheckInDate).TotalDays;

            // Lấy thông tin tất cả phòng
            var roomIds = createBookingDto.Rooms.Select(r => r.RoomID).ToList();
            var rooms = await _context.HotelRooms
                .Include(r => r.Hotel)
                .Where(r => roomIds.Contains(r.RoomID))
                .ToListAsync();

            if (rooms.Count != roomIds.Count)
                throw new InvalidOperationException("Một hoặc nhiều phòng không tồn tại");

            // Kiểm tra tất cả phòng có available không
            var unavailableRooms = rooms.Where(r => r.Status != RoomStatus.Available).ToList();
            if (unavailableRooms.Any())
                throw new InvalidOperationException($"Các phòng sau không có sẵn: {string.Join(", ", unavailableRooms.Select(r => r.RoomName))}");

            // Kiểm tra tất cả phòng cùng khách sạn
            var hotelIds = rooms.Select(r => r.HotelID).Distinct().ToList();
            if (hotelIds.Count > 1)
                throw new InvalidOperationException("Tất cả phòng phải cùng một khách sạn");

            var hotelId = hotelIds.First();

            // Kiểm tra xung đột cho từng phòng
            var conflictingRooms = new List<string>();
            foreach (var roomItem in createBookingDto.Rooms)
            {
                var room = rooms.First(r => r.RoomID == roomItem.RoomID);
                
                // Kiểm tra xung đột cho từng phòng (với quantity)
                var hasConflict = await (
                    from d in _context.BillDetails
                    join b in _context.Bills on d.BillID equals b.BillID
                    join bk in _context.Bookings on b.BookingID equals bk.BookingID
                    where d.RoomID == roomItem.RoomID
                        && bk.Status != BookingStatus.Cancelled
                        && bk.Status != BookingStatus.Refunded
                        && (bk.CheckInDate < createBookingDto.CheckOutDate && bk.CheckOutDate > createBookingDto.CheckInDate)
                    select d.BillDetailID
                ).AnyAsync();

                if (hasConflict)
                {
                    conflictingRooms.Add(room.RoomName);
                }
            }

            // Tạo booking
            var booking = new Booking
            {
                BookingID = Guid.NewGuid(),
                UserID = userId,
                HotelID = hotelId,
                CheckInDate = createBookingDto.CheckInDate,
                CheckOutDate = createBookingDto.CheckOutDate,
                Status = conflictingRooms.Any() ? BookingStatus.Pending : BookingStatus.Confirmed,
                Notes = conflictingRooms.Any() ? $"Có xung đột với phòng: {string.Join(", ", conflictingRooms)}" : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);

            // Tính tổng tiền
            decimal totalPrice = 0;
            foreach (var roomItem in createBookingDto.Rooms)
            {
                var room = rooms.First(r => r.RoomID == roomItem.RoomID);
                var roomTotalPrice = room.Price * numberOfNights; // Mỗi RoomID = 1 phòng
                totalPrice += roomTotalPrice;
            }

            // Tạo Bill
            var bill = new Bill
            {
                BillID = Guid.NewGuid(),
                UserID = booking.UserID,
                BookingID = booking.BookingID,
                Deposit = 0,
                TotalPrice = totalPrice,
                Status = BillStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Bills.Add(bill);

            // Tạo BillDetails cho từng phòng
            foreach (var roomItem in createBookingDto.Rooms)
            {
                var room = rooms.First(r => r.RoomID == roomItem.RoomID);
                var roomTotalPrice = room.Price * numberOfNights; // Mỗi RoomID = 1 phòng

                var billDetail = new BillDetail
                {
                    BillDetailID = Guid.NewGuid(),
                    BillID = bill.BillID,
                    ItemType = TableType.Hotel,
                    ItemID = room.RoomID,
                    RoomID = room.RoomID,
                    ItemName = room.RoomName,
                    Quantity = numberOfNights, // Chỉ lưu số đêm (mỗi RoomID = 1 phòng)
                    UnitPrice = room.Price,
                    TotalPrice = roomTotalPrice,
                    Notes = $"Booking 1 phòng từ {booking.CheckInDate:dd/MM/yyyy} đến {booking.CheckOutDate:dd/MM/yyyy}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.BillDetails.Add(billDetail);
            }

            await _context.SaveChangesAsync();

            return new BookingCreateResult
            {
                BookingID = booking.BookingID,
                BillID = bill.BillID
            };
        }

        // Removed: legacy GetMyBookingsAsync (replaced by GetBookingsPaginatedAsync)

        // Removed: Hotel listing (auto-approve flow)

        // Removed: Confirm endpoint (auto-approve on create)

        // Removed: Cancel by hotel

        public async Task<bool> CustomerCancelBookingAsync(Guid bookingId, Guid userId, string? reason = null)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.UserID == userId);

            if (booking == null)
                return false;

            // Cho phép hủy khi Pending hoặc Confirmed nhưng chưa thanh toán và trước ngày check-in
            var isBeforeCheckIn = DateTime.UtcNow.Date < booking.CheckInDate.Date;
            var isUnpaid = string.IsNullOrEmpty(booking.PaymentStatus) || !string.Equals(booking.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase);
            var canCancel = booking.Status == BookingStatus.Pending || (booking.Status == BookingStatus.Confirmed && isUnpaid && isBeforeCheckIn);

            if (!canCancel)
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

                // Cập nhật Bill (nếu có) về Cancelled
                var bill = await _context.Bills.FirstOrDefaultAsync(x => x.BookingID == booking.BookingID);
                if (bill != null)
                {
                    bill.Status = BillStatus.Cancelled;
                    bill.UpdatedAt = DateTime.UtcNow;
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


        public async Task<BookingDetailResponseDTO?> GetBookingDetailByIdAsync(Guid bookingId, Guid userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.User)
                .Include(b => b.Bill)
                    .ThenInclude(bill => bill.BillDetails)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.UserID == userId);

            if (booking == null)
                return null;

            // Tính số đêm
            var numberOfNights = (int)(booking.CheckOutDate - booking.CheckInDate).TotalDays;

            // Lấy thông tin phòng từ BillDetails
            var roomDetails = new List<BookingRoomDetailDTO>();
            if (booking.Bill?.BillDetails != null)
            {
                foreach (var billDetail in booking.Bill.BillDetails)
                {
                    // Lấy thông tin phòng
                    var room = await _context.HotelRooms
                        .FirstOrDefaultAsync(r => r.RoomID == billDetail.RoomID);

                    if (room != null)
                    {
                        roomDetails.Add(new BookingRoomDetailDTO
                        {
                            RoomID = room.RoomID,
                            RoomName = room.RoomName,
                            RoomType = room.RoomType,
                            Quantity = 1, // Mỗi RoomID = 1 phòng
                            UnitPrice = billDetail.UnitPrice,
                            TotalPrice = billDetail.TotalPrice,
                            Notes = billDetail.Notes
                        });
                    }
                }
            }

            return new BookingDetailResponseDTO
            {
                BookingID = booking.BookingID,
                UserID = booking.UserID,
                UserName = booking.User.FullName ?? booking.User.Email,
                UserEmail = booking.User.Email,
                
                HotelID = booking.HotelID,
                HotelName = booking.Hotel?.HotelName ?? "",
                HotelAddress = booking.Hotel?.Address ?? "",
                
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfNights = numberOfNights,
                Status = booking.Status.ToString(),
                Notes = booking.Notes,
                PaymentMethod = booking.PaymentMethod,
                PaymentStatus = booking.PaymentStatus,
                
                BillID = booking.Bill?.BillID,
                TotalPrice = booking.Bill?.TotalPrice ?? 0,
                Deposit = booking.Bill?.Deposit ?? 0,
                BillStatus = booking.Bill?.Status.ToString() ?? "",
                
                RoomDetails = roomDetails,
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
                .Select(b => new BookingSummaryDTO
                {
                    BookingID = b.BookingID,
                    HotelName = b.Hotel != null ? b.Hotel.HotelName : "",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status.ToString(),
                    PaymentStatus = b.PaymentStatus ?? "",
                    BillID = b.Bill != null ? b.Bill.BillID : (Guid?)null,
                    CreatedAt = b.CreatedAt
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
                .Include(b => b.User)
                .Include(b => b.Bill)
                .AsQueryable();

            // Tìm kiếm theo query (tên khách, email, số điện thoại)
            if (!string.IsNullOrWhiteSpace(searchDto.Query))
            {
                var searchTerm = searchDto.Query.ToLower();
                query = query.Where(b => 
                    (b.User.FullName != null && b.User.FullName.ToLower().Contains(searchTerm)) ||
                    (b.User.Email != null && b.User.Email.ToLower().Contains(searchTerm)) ||
                    (b.User.PhoneNumber != null && b.User.PhoneNumber.Contains(searchTerm))
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

            // Bỏ lọc theo phòng và loại booking vì đã loại khỏi Booking

            // Sắp xếp
            query = searchDto.SortBy?.ToLower() switch
            {
                "checkindate" => searchDto.SortOrder?.ToLower() == "asc" 
                    ? query.OrderBy(b => b.CheckInDate)
                    : query.OrderByDescending(b => b.CheckInDate),
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
                .Select(b => new BookingSummaryDTO
                {
                    BookingID = b.BookingID,
                    HotelName = b.Hotel != null ? b.Hotel.HotelName : "",
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status.ToString(),
                    PaymentStatus = b.PaymentStatus ?? "",
                    BillID = b.Bill != null ? b.Bill.BillID : (Guid?)null,
                    CreatedAt = b.CreatedAt
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

        // Removed: hotel-scoped pagination

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

            // Tìm các booking xung đột dựa vào BillDetail.RoomID
            var conflictingBookings = await (
                from d in _context.BillDetails
                join bl in _context.Bills on d.BillID equals bl.BillID
                join bk in _context.Bookings on bl.BookingID equals bk.BookingID
                where d.RoomID == checkDto.RoomId &&
                      bk.Status != BookingStatus.Cancelled &&
                      bk.Status != BookingStatus.Refunded &&
                      (bk.CheckInDate < checkDto.CheckOutDate && bk.CheckOutDate > checkDto.CheckInDate)
                select new BookingConflictDTO
                {
                    BookingId = bk.BookingID,
                    CheckInDate = bk.CheckInDate,
                    CheckOutDate = bk.CheckOutDate,
                    Status = bk.Status.ToString(),
                }
            ).ToListAsync();

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
                TotalRevenue = 0,
                PendingRevenue = 0,
                ConfirmedRevenue = 0,
                CompletedRevenue = 0
            };

            // Thống kê theo tháng
            var monthlyStats = bookings
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .Select(g => new MonthlyStatsDTO
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Bookings = g.Count(),
                    Revenue = 0
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
                // Tìm bill và booking theo bill ID
                var bill = await _context.Bills.FirstOrDefaultAsync(b => b.BillID == billId);
                if (bill == null)
                    return false;

                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(bk => bk.BookingID == bill.BookingID);

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

        public async Task<QRPaymentResponseDTO> CreateQRPaymentAsync(Guid billId)
        {
            // Kiểm tra Bill có tồn tại không
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.BillID == billId);

            if (bill == null)
                throw new ArgumentException("Không tìm thấy hóa đơn");

            // Tạo QR code đơn giản
            var qrCode = $"QR_PAY_{billId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var paymentUrl = $"https://localhost:7006/api/payments/qr/scan?code={qrCode}";
            var expiresAt = DateTime.UtcNow.AddMinutes(15);

            return new QRPaymentResponseDTO
            {
                QRCode = qrCode,
                PaymentUrl = paymentUrl,
                BillId = billId,
                Amount = bill.TotalPrice,
                OrderDescription = $"Thanh toán hóa đơn #{billId}",
                ExpiresAt = expiresAt
            };
        }

        public async Task<bool> UpdateBookingStatusAsync(Guid billId, string status)
        {
            try
            {
                // Tìm bill và booking theo BillID
                var bill = await _context.Bills.FirstOrDefaultAsync(b => b.BillID == billId);
                if (bill == null)
                    return false;

                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingID == bill.BookingID);

                if (booking == null)
                    return false;

                // Cập nhật trạng thái
                if (Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
                {
                    booking.Status = bookingStatus;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckInBookingAsync(Guid bookingId, Guid userId)
        {
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.UserID == userId);

                if (booking == null)
                    return false;

                // Chỉ cho phép check-in nếu booking đã confirmed và đã thanh toán
                if (booking.Status != BookingStatus.Confirmed || booking.PaymentStatus != "Paid")
                    return false;

                // Cập nhật trạng thái booking
                booking.Status = BookingStatus.Completed;
                booking.UpdatedAt = DateTime.UtcNow;

                // Cập nhật trạng thái phòng thành Occupied qua BillDetail
                var bill = await _context.Bills.FirstOrDefaultAsync(x => x.BookingID == booking.BookingID);
                if (bill != null)
                {
                    var detail = await _context.BillDetails.FirstOrDefaultAsync(d => d.BillID == bill.BillID && d.RoomID != null);
                    if (detail?.RoomID != null)
                    {
                        var theRoom = await _context.HotelRooms.FirstOrDefaultAsync(r => r.RoomID == detail.RoomID);
                        if (theRoom != null)
                        {
                            theRoom.Status = RoomStatus.Occupied;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckOutBookingAsync(Guid bookingId, Guid userId)
        {
            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.UserID == userId);

                if (booking == null)
                    return false;

                // Chỉ cho phép check-out nếu booking đã completed
                if (booking.Status != BookingStatus.Completed)
                    return false;

                // Cập nhật trạng thái phòng thành Available qua BillDetail
                var bill = await _context.Bills.FirstOrDefaultAsync(x => x.BookingID == booking.BookingID);
                if (bill != null)
                {
                    var detail = await _context.BillDetails.FirstOrDefaultAsync(d => d.BillID == bill.BillID && d.RoomID != null);
                    if (detail?.RoomID != null)
                    {
                        var theRoom = await _context.HotelRooms.FirstOrDefaultAsync(r => r.RoomID == detail.RoomID);
                        if (theRoom != null)
                        {
                            theRoom.Status = RoomStatus.Available;
                        }
                    }
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
