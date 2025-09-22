using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services
{
    public class RefundService : IRefundService
    {
        private readonly ApplicationDbContext _context;

        public RefundService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> CreateRefundAsync(Guid userId, CreateRefundDTO createRefundDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Kiểm tra bill có tồn tại và thuộc về user không
                var bill = await _context.Bills
                    .Include(b => b.Booking)
                    .FirstOrDefaultAsync(b => b.BillID == createRefundDto.BillID && b.UserID == userId);

                if (bill == null)
                    throw new ArgumentException("Hóa đơn không tồn tại hoặc không thuộc về người dùng này");

                // Kiểm tra bill có thể refund không
                if (bill.Status != BillStatus.Paid)
                    throw new ArgumentException("Chỉ có thể hoàn tiền cho hóa đơn đã thanh toán");

                // Kiểm tra booking chưa bắt đầu
                if (bill.Booking != null && bill.Booking.CheckInDate <= DateTime.UtcNow)
                    throw new ArgumentException("Không thể hoàn tiền cho booking đã bắt đầu");

                // Kiểm tra đã có refund request chưa (Pending)
                var existingRefund = await _context.Refunds
                    .FirstOrDefaultAsync(r => r.BillID == createRefundDto.BillID && r.Status == RefundStatus.Pending);

                if (existingRefund != null)
                    throw new ArgumentException("Đã có yêu cầu hoàn tiền đang chờ duyệt cho hóa đơn này");

                // Tính toán refund theo chính sách thời gian
                var refundInfo = CalculateRefundAmount(bill);
                
                // Tạo refund với status Pending (chờ duyệt)
                var refund = new Refund
                {
                    RefundID = Guid.NewGuid(),
                    BillID = createRefundDto.BillID,
                    Description = $"{createRefundDto.Description}\n[Refund Policy: {refundInfo.Percentage}% - {refundInfo.PolicyDescription}]",
                    Status = RefundStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Refunds.Add(refund);
                // KHÔNG cập nhật Bill/Booking ở bước tạo yêu cầu (đợi approve)

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return refund.RefundID;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Tạo yêu cầu refund: tái sử dụng CreateRefundAsync để tạo Refund Pending trong DB
        public async Task<Guid> CreateRefundRequestAsync(Guid userId, CreateRefundDTO createRefundDto)
        {
            return await CreateRefundAsync(userId, createRefundDto);
        }

        // Phê duyệt: lấy Refund Pending trong DB, sau đó cập nhật Bill/Booking và set Completed
        public async Task<Guid> ApproveRefundRequestAsync(Guid billId, Guid approverId)
        {
            var pending = await _context.Refunds
                .Include(r => r.Bill)
                    .ThenInclude(b => b.Booking)
                .FirstOrDefaultAsync(r => r.BillID == billId && r.Status == RefundStatus.Pending);
            if (pending == null) throw new ArgumentException("Không có yêu cầu hoàn tiền đang chờ duyệt");

            // Kiểm tra quyền: chủ Hotel/Tour hoặc Supervisor/Admin
            var approver = await _context.Users.FindAsync(approverId);
            var isSupervisorOrAdmin = approver != null && (approver.Role == RoleConstants.Supervisor || approver.Role == RoleConstants.Admin);

            // Xác định owner theo booking
            Guid? hotelOwnerId = null; Guid? tourOwnerId = null;
            if (pending.Bill.Booking?.HotelID != null)
            {
                var hotel = await _context.Hotels.FindAsync(pending.Bill.Booking.HotelID.Value);
                hotelOwnerId = hotel?.UserID;
            }
            if (pending.Bill.Booking?.TourID != null)
            {
                var tour = await _context.Tours.FindAsync(pending.Bill.Booking.TourID.Value);
                tourOwnerId = tour?.UserID;
            }

            bool authorized = isSupervisorOrAdmin
                || (hotelOwnerId.HasValue && hotelOwnerId.Value == approverId)
                || (tourOwnerId.HasValue && tourOwnerId.Value == approverId);

            if (!authorized)
                throw new ArgumentException("Bạn không có quyền duyệt yêu cầu hoàn tiền này");

            // Thực hiện hoàn tiền thật sự và cập nhật DB
            var bill = await _context.Bills
                .Include(b => b.Booking)
                .FirstAsync(b => b.BillID == billId);

            var refundInfo = CalculateRefundAmount(bill);
            bill.RefundPrice = refundInfo.RefundAmount;
            bill.UpdatedAt = DateTime.UtcNow;
            bill.Status = refundInfo.Percentage == 100 ? BillStatus.Cancelled : BillStatus.Refunded;

            if (bill.Booking != null)
            {
                bill.Booking.Status = BookingStatus.Cancelled;
                if (bill.Booking.HotelID != null) await ProcessHotelRefundAsync(bill.Booking);
                if (bill.Booking.TourID != null) await ProcessTourRefundAsync(bill.Booking);
            }

            pending.Status = RefundStatus.Completed;
            await _context.SaveChangesAsync();
            return pending.RefundID;
        }

        // Từ chối: xóa Refund Pending khỏi DB
        public async Task<bool> RejectRefundRequestAsync(Guid billId, Guid approverId, string? reason = null)
        {
            var pending = await _context.Refunds.FirstOrDefaultAsync(r => r.BillID == billId && r.Status == RefundStatus.Pending);
            if (pending == null) return false;

            // Kiểm tra quyền tương tự approve
            var approver = await _context.Users.FindAsync(approverId);
            var isSupervisorOrAdmin = approver != null && (approver.Role == RoleConstants.Supervisor || approver.Role == RoleConstants.Admin);
            // Check quyền theo owner booking
            Guid? hotelOwnerId = null; Guid? tourOwnerId = null;
            if (pending.BillID != Guid.Empty)
            {
                var bill = await _context.Bills.Include(b => b.Booking).FirstOrDefaultAsync(b => b.BillID == billId);
                if (bill?.Booking?.HotelID != null)
                {
                    var hotel = await _context.Hotels.FindAsync(bill.Booking.HotelID.Value);
                    hotelOwnerId = hotel?.UserID;
                }
                if (bill?.Booking?.TourID != null)
                {
                    var tour = await _context.Tours.FindAsync(bill.Booking.TourID.Value);
                    tourOwnerId = tour?.UserID;
                }
            }

            bool authorized = isSupervisorOrAdmin
                || (hotelOwnerId.HasValue && hotelOwnerId.Value == approverId)
                || (tourOwnerId.HasValue && tourOwnerId.Value == approverId);
            if (!authorized)
                throw new ArgumentException("Bạn không có quyền từ chối yêu cầu hoàn tiền này");

            _context.Refunds.Remove(pending);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<RefundResponseDTO?> GetRefundByIdAsync(Guid refundId, Guid userId)
        {
            var refund = await _context.Refunds
                .Include(r => r.Bill)
                    .ThenInclude(b => b.User)
                .Include(r => r.Bill)
                    .ThenInclude(b => b.Booking)
                .FirstOrDefaultAsync(r => r.RefundID == refundId);

            if (refund == null)
                return null;

            // Kiểm tra quyền truy cập (user chỉ xem được refund của mình, admin xem được tất cả)
            if (refund.Bill.UserID != userId)
            {
                // Kiểm tra user có phải admin không
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.Role != "Admin")
                    return null;
            }

            return MapToRefundResponseDTO(refund);
        }

        public async Task<RefundListResponseDTO> GetUserRefundsAsync(Guid userId, int page = 1, int size = 10)
        {
            var query = _context.Refunds
                .Include(r => r.Bill)
                    .ThenInclude(b => b.User)
                .Include(r => r.Bill)
                    .ThenInclude(b => b.Booking)
                .Where(r => r.Bill.UserID == userId)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var refundEntities = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var refunds = refundEntities
                .Select(r => MapToRefundResponseDTO(r))
                .ToList();

            return new RefundListResponseDTO
            {
                Refunds = refunds,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }

        public async Task<RefundListResponseDTO> GetAllRefundsAsync(int page = 1, int size = 10)
        {
            var query = _context.Refunds
                .Include(r => r.Bill)
                    .ThenInclude(b => b.User)
                .Include(r => r.Bill)
                    .ThenInclude(b => b.Booking)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var refundEntities = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var refunds = refundEntities
                .Select(r => MapToRefundResponseDTO(r))
                .ToList();

            return new RefundListResponseDTO
            {
                Refunds = refunds,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages
            };
        }


        public async Task<RefundStatsDTO> GetRefundStatsAsync()
        {
            var totalRefunds = await _context.Refunds.CountAsync();
            var totalRefundAmount = await _context.Refunds
                .Include(r => r.Bill)
                .SumAsync(r => r.Bill.TotalPrice);

            return new RefundStatsDTO
            {
                TotalRefunds = totalRefunds,
                PendingRefunds = 0, // Không còn pending vì tự động approve
                ApprovedRefunds = totalRefunds, // Tất cả đều completed
                DeniedRefunds = 0, // Không còn deny
                TotalRefundAmount = totalRefundAmount,
                PendingRefundAmount = 0 // Không còn pending
            };
        }


        private RefundInfo CalculateRefundAmount(Bill bill)
        {
            var totalPrice = bill.TotalPrice;
            var departureDate = GetDepartureDate(bill);
            var daysUntilDeparture = (departureDate - DateTime.UtcNow).Days;

            int percentage;
            string policyDescription;

            if (daysUntilDeparture < 3)
            {
                // < 3 ngày trước xuất phát: Refund 10%
                percentage = 10;
                policyDescription = "Dưới 3 ngày trước xuất phát";
            }
            else if (daysUntilDeparture < 7)
            {
                // 3-7 ngày trước xuất phát: Refund 50%
                percentage = 50;
                policyDescription = "Từ 3-7 ngày trước xuất phát";
            }
            else
            {
                // ≥ 7 ngày trước xuất phát: Refund 100%
                percentage = 100;
                policyDescription = "Trên 7 ngày trước xuất phát";
            }

            var refundAmount = totalPrice * percentage / 100;

            return new RefundInfo
            {
                Percentage = percentage,
                RefundAmount = refundAmount,
                PolicyDescription = policyDescription,
                DaysUntilDeparture = daysUntilDeparture
            };
        }

        private DateTime GetDepartureDate(Bill bill)
        {
            // Lấy ngày xuất phát từ booking
            if (bill.Booking != null)
            {
                // Sử dụng CheckInDate làm ngày xuất phát
                return bill.Booking.CheckInDate;
            }

            // Nếu không có booking, lấy từ bill details (schedule)
            var scheduleDetail = bill.BillDetails.FirstOrDefault(bd => bd.ScheduleID != null);
            if (scheduleDetail?.ScheduleID != null)
            {
                // Cần query schedule để lấy ngày xuất phát
                // Tạm thời return ngày hiện tại + 1 ngày
                return DateTime.UtcNow.AddDays(1);
            }

            // Fallback: return ngày hiện tại + 1 ngày
            return DateTime.UtcNow.AddDays(1);
        }

        private RefundResponseDTO MapToRefundResponseDTO(Refund refund)
        {
            // Tính toán thông tin refund policy từ description
            var refundInfo = ExtractRefundInfoFromDescription(refund.Description);
            
            return new RefundResponseDTO
            {
                RefundID = refund.RefundID,
                BillID = refund.BillID,
                Description = refund.Description,
                Status = refund.Status.ToString(),
                CreatedAt = refund.CreatedAt,
                RefundPercentage = refundInfo.Percentage,
                RefundAmount = refund.Bill.RefundPrice ?? 0,
                PolicyDescription = refundInfo.PolicyDescription,
                DaysUntilDeparture = refundInfo.DaysUntilDeparture,
                BillInfo = new BillInfoDTO
                {
                    BillID = refund.Bill.BillID,
                    TotalPrice = refund.Bill.TotalPrice,
                    RefundPrice = refund.Bill.RefundPrice,
                    Status = refund.Bill.Status.ToString(),
                    CreatedAt = refund.Bill.CreatedAt
                },
                UserInfo = new UserInfoDTO
                {
                    UserID = refund.Bill.UserID,
                    UserName = refund.Bill.User.FullName,
                    Email = refund.Bill.User.Email
                }
            };
        }

        private RefundInfo ExtractRefundInfoFromDescription(string description)
        {
            // Parse thông tin từ description để lấy refund policy
            // Format: "...\n[Refund Policy: 50% - Từ 3-7 ngày trước xuất phát]"
            var lines = description.Split('\n');
            var policyLine = lines.FirstOrDefault(l => l.Contains("[Refund Policy:"));
            
            if (policyLine != null)
            {
                // Extract percentage
                var percentageMatch = System.Text.RegularExpressions.Regex.Match(policyLine, @"(\d+)%");
                var percentage = percentageMatch.Success ? int.Parse(percentageMatch.Groups[1].Value) : 0;
                
                // Extract policy description
                var descMatch = System.Text.RegularExpressions.Regex.Match(policyLine, @"- (.+)\]");
                var policyDesc = descMatch.Success ? descMatch.Groups[1].Value : "";
                
                return new RefundInfo
                {
                    Percentage = percentage,
                    PolicyDescription = policyDesc,
                    DaysUntilDeparture = 0 // Không lưu trong description
                };
            }
            
            return new RefundInfo
            {
                Percentage = 0,
                PolicyDescription = "Không xác định",
                DaysUntilDeparture = 0
            };
        }

        // Xử lý refund cho Hotel booking
        private async Task ProcessHotelRefundAsync(Booking booking)
        {
            // Lấy BillDetails để tìm các phòng đã đặt
            var billDetails = await _context.BillDetails
                .Where(bd => bd.BillID == booking.BookingID && bd.ItemType == TableType.HotelRoom)
                .ToListAsync();

            foreach (var billDetail in billDetails)
            {
                // Cập nhật trạng thái phòng thành Available
                var room = await _context.HotelRooms.FindAsync(billDetail.ItemID);
                if (room != null)
                {
                    room.Status = RoomStatus.Available;
                }
            }
        }

        // Xử lý refund cho Tour booking (Schedule)
        private async Task ProcessTourRefundAsync(Booking booking)
        {
            // Lấy BillDetails để tìm schedule và số người đã đặt
            var billDetails = await _context.BillDetails
                .Where(bd => bd.BillID == booking.BookingID && bd.ItemType == TableType.Schedule)
                .ToListAsync();

            foreach (var billDetail in billDetails)
            {
                // Giảm số người trong schedule
                var schedule = await _context.Schedules.FindAsync(billDetail.ItemID);
                if (schedule != null)
                {
                    // Giảm số người đã đặt
                    schedule.CurrentBookings = Math.Max(0, schedule.CurrentBookings - billDetail.Quantity);
                    
                    Console.WriteLine($"Schedule {schedule.ScheduleID} refunded {billDetail.Quantity} guests. Current bookings: {schedule.CurrentBookings}/{schedule.NumberPeople}");
                }
            }
        }
    }

    // Helper class cho thông tin refund
    public class RefundInfo
    {
        public int Percentage { get; set; }
        public decimal RefundAmount { get; set; }
        public string PolicyDescription { get; set; } = string.Empty;
        public int DaysUntilDeparture { get; set; }
    }
}
