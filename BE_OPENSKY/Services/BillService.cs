using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services
{
    public class BillService : IBillService
    {
        private readonly ApplicationDbContext _context;

        public BillService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BillResponseDTO?> GetBillByIdAsync(Guid billId, Guid userId)
        {
            var bill = await _context.Bills
                .Include(b => b.User)
                .Include(b => b.BillDetails)
                .FirstOrDefaultAsync(b => b.BillID == billId && b.UserID == userId);

            if (bill == null)
                return null;

            return new BillResponseDTO
            {
                BillID = bill.BillID,
                UserID = bill.UserID,
                UserName = bill.User.FullName,
                TableType = bill.TableType.ToString(),
                TypeID = bill.TypeID,
                Deposit = bill.Deposit,
                TotalPrice = bill.TotalPrice,
                Status = bill.Status.ToString(),
                CreatedAt = bill.CreatedAt,
                BillDetails = bill.BillDetails.Select(bd => new BillDetailResponseDTO
                {
                    BillDetailID = bd.BillDetailID,
                    BillID = bd.BillID,
                    ItemType = bd.ItemType.ToString(),
                    ItemID = bd.ItemID,
                    ItemName = bd.ItemName,
                    Quantity = bd.Quantity,
                    UnitPrice = bd.UnitPrice,
                    TotalPrice = bd.TotalPrice,
                    Notes = bd.Notes,
                    CreatedAt = bd.CreatedAt
                }).ToList()
            };
        }

        public async Task<List<BillResponseDTO>> GetUserBillsAsync(Guid userId)
        {
            var bills = await _context.Bills
                .Include(b => b.User)
                .Include(b => b.BillDetails)
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bills.Select(b => new BillResponseDTO
            {
                BillID = b.BillID,
                UserID = b.UserID,
                UserName = b.User.FullName,
                TableType = b.TableType.ToString(),
                TypeID = b.TypeID,
                Deposit = b.Deposit,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                CreatedAt = b.CreatedAt,
                BillDetails = b.BillDetails.Select(bd => new BillDetailResponseDTO
                {
                    BillDetailID = bd.BillDetailID,
                    BillID = bd.BillID,
                    ItemType = bd.ItemType.ToString(),
                    ItemID = bd.ItemID,
                    ItemName = bd.ItemName,
                    Quantity = bd.Quantity,
                    UnitPrice = bd.UnitPrice,
                    TotalPrice = bd.TotalPrice,
                    Notes = bd.Notes,
                    CreatedAt = bd.CreatedAt
                }).ToList()
            }).ToList();
        }

        public async Task<bool> UpdateBillPaymentStatusAsync(Guid billId, string paymentMethod, string transactionId, decimal amount)
        {
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.BillID == billId);

            if (bill == null)
                return false;

            // Cập nhật trạng thái bill
            bill.Status = BillStatus.Paid;
            bill.UpdatedAt = DateTime.UtcNow;

            // Cập nhật thông tin thanh toán nếu cần
            // bill.PaymentMethod = paymentMethod;
            // bill.TransactionId = transactionId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BillResponseDTO?> GetBillByBookingIdAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Bill)
                .ThenInclude(bill => bill!.User)
                .Include(b => b.Bill)
                .ThenInclude(bill => bill!.BillDetails)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking?.Bill == null)
                return null;

            var bill = booking.Bill;
            return new BillResponseDTO
            {
                BillID = bill.BillID,
                UserID = bill.UserID,
                UserName = bill.User.FullName,
                TableType = bill.TableType.ToString(),
                TypeID = bill.TypeID,
                Deposit = bill.Deposit,
                TotalPrice = bill.TotalPrice,
                Status = bill.Status.ToString(),
                CreatedAt = bill.CreatedAt,
                BillDetails = bill.BillDetails.Select(bd => new BillDetailResponseDTO
                {
                    BillDetailID = bd.BillDetailID,
                    BillID = bd.BillID,
                    ItemType = bd.ItemType.ToString(),
                    ItemID = bd.ItemID,
                    ItemName = bd.ItemName,
                    Quantity = bd.Quantity,
                    UnitPrice = bd.UnitPrice,
                    TotalPrice = bd.TotalPrice,
                    Notes = bd.Notes,
                    CreatedAt = bd.CreatedAt
                }).ToList()
            };
        }
    }
}
