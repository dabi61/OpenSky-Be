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

                // Kiểm tra đã có refund request chưa
                var existingRefund = await _context.Refunds
                    .FirstOrDefaultAsync(r => r.BillID == createRefundDto.BillID);

                if (existingRefund != null)
                    throw new ArgumentException("Đã có yêu cầu hoàn tiền cho hóa đơn này");

                // Tính toán refund theo chính sách thời gian
                var refundInfo = CalculateRefundAmount(bill);
                
                // Tạo refund với status Completed (tự động hoàn tiền)
                var refund = new Refund
                {
                    RefundID = Guid.NewGuid(),
                    BillID = createRefundDto.BillID,
                    Description = $"{createRefundDto.Description}\n[Refund Policy: {refundInfo.Percentage}% - {refundInfo.PolicyDescription}]",
                    Status = RefundStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Refunds.Add(refund);

                // Cập nhật bill theo chính sách refund
                bill.RefundPrice = refundInfo.RefundAmount;
                bill.UpdatedAt = DateTime.UtcNow;

                // Xác định BillStatus dựa trên % refund
                if (refundInfo.Percentage == 100)
                {
                    // Refund 100% → BillStatus = Cancelled
                    bill.Status = BillStatus.Cancelled;
                }
                else
                {
                    // Refund < 100% → BillStatus = Refunded
                    bill.Status = BillStatus.Refunded;
                }

                // Cập nhật trạng thái booking thành Cancelled
                if (bill.Booking != null)
                {
                    bill.Booking.Status = BookingStatus.Cancelled;
                }

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

            var refunds = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(r => MapToRefundResponseDTO(r))
                .ToListAsync();

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

            var refunds = await query
                .Skip((page - 1) * size)
                .Take(size)
                .Select(r => MapToRefundResponseDTO(r))
                .ToListAsync();

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
