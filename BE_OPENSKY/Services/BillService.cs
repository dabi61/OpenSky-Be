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
                .Include(b => b.Booking)
                .Include(b => b.BillDetails)
                .Include(b => b.UserVoucher)
                    .ThenInclude(uv => uv.Voucher)
                .FirstOrDefaultAsync(b => b.BillID == billId && b.UserID == userId);

            if (bill == null)
                return null;

            // Tính toán thông tin giảm giá
            var originalTotalPrice = bill.BillDetails?.Sum(bd => bd.TotalPrice) ?? 0;
            var discountAmount = originalTotalPrice - bill.TotalPrice;
            var discountPercent = originalTotalPrice > 0 ? (discountAmount / originalTotalPrice) * 100 : 0;

            // Thông tin voucher
            VoucherInfoDTO? voucherInfo = null;
            if (bill.UserVoucher?.Voucher != null)
            {
                voucherInfo = new VoucherInfoDTO
                {
                    Code = bill.UserVoucher.Voucher.Code,
                    Percent = bill.UserVoucher.Voucher.Percent,
                    TableType = bill.UserVoucher.Voucher.TableType,
                    Description = bill.UserVoucher.Voucher.Description
                };
            }

            return new BillResponseDTO
            {
                BillID = bill.BillID,
                UserID = bill.UserID,
                UserName = bill.User.FullName,
                BookingID = bill.BookingID,
                StartTime = bill.Booking?.CheckInDate,
                EndTime = bill.Booking?.CheckOutDate,
                Deposit = bill.Deposit,
                RefundPrice = bill.RefundPrice,
                TotalPrice = bill.TotalPrice,
                OriginalTotalPrice = originalTotalPrice,
                DiscountAmount = discountAmount,
                DiscountPercent = discountPercent,
                Status = bill.Status.ToString(),
                CreatedAt = bill.CreatedAt,
                UpdatedAt = bill.UpdatedAt,
                UserVoucherID = bill.UserVoucherID,
                VoucherInfo = voucherInfo,
                User = new UserInfoDTO
                {
                    UserID = bill.User.UserID,
                    FullName = bill.User.FullName,
                    Email = bill.User.Email,
                    PhoneNumber = bill.User.PhoneNumber,
                    CitizenId = bill.User.CitizenId
                },
                BillDetails = bill.BillDetails?.Select(bd => new BillDetailResponseDTO
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
                }).ToList() ?? new List<BillDetailResponseDTO>()
            };
        }

        public async Task<BillResponseDTO?> GetBillByIdAsAdminAsync(Guid billId)
        {
            var bill = await _context.Bills
                .Include(b => b.User)
                .Include(b => b.Booking)
                .Include(b => b.BillDetails)
                .Include(b => b.UserVoucher)
                    .ThenInclude(uv => uv.Voucher)
                .FirstOrDefaultAsync(b => b.BillID == billId);

            if (bill == null)
                return null;

            var originalTotalPrice = bill.BillDetails?.Sum(bd => bd.TotalPrice) ?? 0;
            var discountAmount = originalTotalPrice - bill.TotalPrice;
            var discountPercent = originalTotalPrice > 0 ? (discountAmount / originalTotalPrice) * 100 : 0;

            VoucherInfoDTO? voucherInfo = null;
            if (bill.UserVoucher?.Voucher != null)
            {
                voucherInfo = new VoucherInfoDTO
                {
                    Code = bill.UserVoucher.Voucher.Code,
                    Percent = bill.UserVoucher.Voucher.Percent,
                    TableType = bill.UserVoucher.Voucher.TableType,
                    Description = bill.UserVoucher.Voucher.Description
                };
            }

            return new BillResponseDTO
            {
                BillID = bill.BillID,
                UserID = bill.UserID,
                UserName = bill.User?.FullName ?? string.Empty,
                BookingID = bill.BookingID,
                StartTime = bill.Booking?.CheckInDate,
                EndTime = bill.Booking?.CheckOutDate,
                Deposit = bill.Deposit,
                RefundPrice = bill.RefundPrice,
                TotalPrice = bill.TotalPrice,
                OriginalTotalPrice = originalTotalPrice,
                DiscountAmount = discountAmount,
                DiscountPercent = discountPercent,
                Status = bill.Status.ToString(),
                CreatedAt = bill.CreatedAt,
                UpdatedAt = bill.UpdatedAt,
                UserVoucherID = bill.UserVoucherID,
                VoucherInfo = voucherInfo,
                User = new UserInfoDTO
                {
                    UserID = bill.User?.UserID ?? Guid.Empty,
                    FullName = bill.User?.FullName ?? string.Empty,
                    Email = bill.User?.Email ?? string.Empty,
                    PhoneNumber = bill.User?.PhoneNumber,
                    CitizenId = bill.User?.CitizenId
                },
                BillDetails = bill.BillDetails?.Select(bd => new BillDetailResponseDTO
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
                }).ToList() ?? new List<BillDetailResponseDTO>()
            };
        }

        public async Task<ApplyVoucherResponseDTO> ApplyVoucherToBillAsync(Guid userId, ApplyVoucherToBillDTO applyVoucherDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lấy bill
                var bill = await _context.Bills
                    .Include(b => b.BillDetails)
                    .Include(b => b.UserVoucher)
                        .ThenInclude(uv => uv.Voucher)
                    .FirstOrDefaultAsync(b => b.BillID == applyVoucherDto.BillID && b.UserID == userId);

                if (bill == null)
                    throw new ArgumentException("Hóa đơn không tồn tại hoặc không thuộc về người dùng này");

                if (bill.Status != BillStatus.Pending)
                    throw new ArgumentException("Chỉ có thể áp dụng voucher cho hóa đơn đang chờ thanh toán");

                // Lấy voucher
                var userVoucher = await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .FirstOrDefaultAsync(uv => uv.UserVoucherID == applyVoucherDto.UserVoucherID && uv.UserID == userId);

                if (userVoucher == null)
                    throw new ArgumentException("Voucher không tồn tại hoặc không thuộc về người dùng này");

                if (userVoucher.IsUsed)
                    throw new ArgumentException("Voucher đã được sử dụng");

                if (DateTime.UtcNow > userVoucher.Voucher.EndDate)
                    throw new ArgumentException("Voucher đã hết hạn");

                // Kiểm tra loại voucher có phù hợp không
                var hasMatchingItem = bill.BillDetails.Any(bd => bd.ItemType == userVoucher.Voucher.TableType);
                if (!hasMatchingItem)
                    throw new ArgumentException("Voucher không áp dụng cho loại dịch vụ trong hóa đơn này");

                // Tính toán giá
                var originalTotalPrice = bill.BillDetails.Sum(bd => bd.TotalPrice);
                var discountPercent = userVoucher.Voucher.Percent;
                var discountAmount = originalTotalPrice * discountPercent / 100;
                var newTotalPrice = originalTotalPrice - discountAmount;

                // Cập nhật bill
                bill.UserVoucherID = applyVoucherDto.UserVoucherID;
                bill.TotalPrice = newTotalPrice;
                bill.UpdatedAt = DateTime.UtcNow;

                // Đánh dấu voucher đã sử dụng
                userVoucher.IsUsed = true;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ApplyVoucherResponseDTO
                {
                    BillID = bill.BillID,
                    OriginalTotalPrice = originalTotalPrice,
                    NewTotalPrice = newTotalPrice,
                    DiscountAmount = discountAmount,
                    DiscountPercent = discountPercent,
                    VoucherInfo = new VoucherInfoDTO
                    {
                        Code = userVoucher.Voucher.Code,
                        Percent = userVoucher.Voucher.Percent,
                        TableType = userVoucher.Voucher.TableType,
                        Description = userVoucher.Voucher.Description
                    },
                    Message = $"Áp dụng voucher thành công! Giảm {discountPercent}% ({discountAmount:N0} VNĐ)"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ApplyVoucherResponseDTO> RemoveVoucherFromBillAsync(Guid billId, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lấy bill
                var bill = await _context.Bills
                    .Include(b => b.BillDetails)
                    .Include(b => b.UserVoucher)
                        .ThenInclude(uv => uv.Voucher)
                    .FirstOrDefaultAsync(b => b.BillID == billId && b.UserID == userId);

                if (bill == null)
                    throw new ArgumentException("Hóa đơn không tồn tại hoặc không thuộc về người dùng này");

                if (bill.Status != BillStatus.Pending)
                    throw new ArgumentException("Chỉ có thể xóa voucher khỏi hóa đơn đang chờ thanh toán");

                if (bill.UserVoucherID == null)
                    throw new ArgumentException("Hóa đơn này chưa có voucher nào được áp dụng");

                // Lưu thông tin voucher để trả về
                var voucherInfo = bill.UserVoucher?.Voucher != null ? new VoucherInfoDTO
                {
                    Code = bill.UserVoucher.Voucher.Code,
                    Percent = bill.UserVoucher.Voucher.Percent,
                    TableType = bill.UserVoucher.Voucher.TableType,
                    Description = bill.UserVoucher.Voucher.Description
                } : null;

                // Tính toán giá
                var originalTotalPrice = bill.BillDetails.Sum(bd => bd.TotalPrice);
                var currentDiscountAmount = originalTotalPrice - bill.TotalPrice;
                var newTotalPrice = originalTotalPrice;

                // Cập nhật bill
                var oldUserVoucherId = bill.UserVoucherID;
                bill.UserVoucherID = null;
                bill.TotalPrice = newTotalPrice;
                bill.UpdatedAt = DateTime.UtcNow;

                // Đánh dấu voucher chưa sử dụng lại
                if (oldUserVoucherId.HasValue)
                {
                    var oldUserVoucher = await _context.UserVouchers
                        .FirstOrDefaultAsync(uv => uv.UserVoucherID == oldUserVoucherId.Value);
                    if (oldUserVoucher != null)
                    {
                        oldUserVoucher.IsUsed = false;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ApplyVoucherResponseDTO
                {
                    BillID = bill.BillID,
                    OriginalTotalPrice = originalTotalPrice,
                    NewTotalPrice = newTotalPrice,
                    DiscountAmount = 0,
                    DiscountPercent = 0,
                    VoucherInfo = voucherInfo,
                    Message = "Đã xóa voucher khỏi hóa đơn"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<BillResponseDTO>> GetUserBillsAsync(Guid userId)
        {
            var bills = await _context.Bills
                .Include(b => b.User)
                .Include(b => b.Booking)
                .Include(b => b.BillDetails)
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bills.Select(b => new BillResponseDTO
            {
                BillID = b.BillID,
                UserID = b.UserID,
                UserName = b.User.FullName,
                BookingID = b.BookingID,
                StartTime = b.Booking?.CheckInDate,
                EndTime = b.Booking?.CheckOutDate,
                Deposit = b.Deposit,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                CreatedAt = b.CreatedAt,
                User = new UserInfoDTO
                {
                    UserID = b.User.UserID,
                    FullName = b.User.FullName,
                    Email = b.User.Email,
                    PhoneNumber = b.User.PhoneNumber,
                    CitizenId = b.User.CitizenId
                },
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
                }).ToList() ?? new List<BillDetailResponseDTO>()
            }).ToList();
        }

        public async Task<BillListResponseDTO> GetUserBillsPaginatedAsync(Guid userId, int page = 1, int size = 10)
        {
            var query = _context.Bills
                .Include(b => b.User)
                .Include(b => b.Booking)
                .Include(b => b.BillDetails)
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var bills = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var billDtos = bills.Select(b => new BillResponseDTO
            {
                BillID = b.BillID,
                UserID = b.UserID,
                UserName = b.User.FullName,
                BookingID = b.BookingID,
                StartTime = b.Booking?.CheckInDate,
                EndTime = b.Booking?.CheckOutDate,
                Deposit = b.Deposit,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                CreatedAt = b.CreatedAt,
                User = new UserInfoDTO
                {
                    UserID = b.User.UserID,
                    FullName = b.User.FullName,
                    Email = b.User.Email,
                    PhoneNumber = b.User.PhoneNumber,
                    CitizenId = b.User.CitizenId
                },
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
                }).ToList() ?? new List<BillDetailResponseDTO>()
            }).ToList();

            return new BillListResponseDTO
            {
                Bills = billDtos,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<BillListResponseDTO> GetAllBillsPaginatedAsync(int page = 1, int size = 10)
        {
            var query = _context.Bills
                .Include(b => b.User)
                .Include(b => b.Booking)
                .Include(b => b.BillDetails)
                .Include(b => b.UserVoucher)
                    .ThenInclude(uv => uv.Voucher)
                .OrderByDescending(b => b.CreatedAt);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            var bills = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var billDtos = bills.Select(b => 
            {
                // Tính toán thông tin giảm giá
                var originalTotalPrice = b.BillDetails?.Sum(bd => bd.TotalPrice) ?? 0;
                var discountAmount = originalTotalPrice - b.TotalPrice;
                var discountPercent = originalTotalPrice > 0 ? (discountAmount / originalTotalPrice) * 100 : 0;

                // Thông tin voucher
                VoucherInfoDTO? voucherInfo = null;
                if (b.UserVoucher?.Voucher != null)
                {
                    voucherInfo = new VoucherInfoDTO
                    {
                        Code = b.UserVoucher.Voucher.Code,
                        Percent = b.UserVoucher.Voucher.Percent,
                        TableType = b.UserVoucher.Voucher.TableType,
                        Description = b.UserVoucher.Voucher.Description
                    };
                }

                return new BillResponseDTO
                {
                    BillID = b.BillID,
                    UserID = b.UserID,
                    UserName = b.User.FullName,
                    BookingID = b.BookingID,
                    StartTime = b.Booking?.CheckInDate,
                    EndTime = b.Booking?.CheckOutDate,
                    Deposit = b.Deposit,
                    RefundPrice = b.RefundPrice,
                    TotalPrice = b.TotalPrice,
                    OriginalTotalPrice = originalTotalPrice,
                    DiscountAmount = discountAmount,
                    DiscountPercent = discountPercent,
                    Status = b.Status.ToString(),
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    UserVoucherID = b.UserVoucherID,
                    VoucherInfo = voucherInfo,
                    User = new UserInfoDTO
                    {
                        UserID = b.User.UserID,
                        FullName = b.User.FullName,
                        Email = b.User.Email,
                        PhoneNumber = b.User.PhoneNumber,
                        CitizenId = b.User.CitizenId
                    },
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
                    }).ToList() ?? new List<BillDetailResponseDTO>()
                };
            }).ToList();

            return new BillListResponseDTO
            {
                Bills = billDtos,
                TotalCount = totalCount,
                Page = page,
                Size = size,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<bool> UpdateBillPaymentStatusAsync(Guid billId, string paymentMethod, string transactionId, decimal amount)
        {
            var bill = await _context.Bills
                .Include(b => b.BillDetails)
                .FirstOrDefaultAsync(b => b.BillID == billId);

            if (bill == null)
                return false;

            // Nếu đã paid trước đó thì không làm gì thêm (idempotent)
            if (bill.Status == BillStatus.Paid)
                return true;

            // Cập nhật trạng thái bill
            bill.Status = BillStatus.Paid;
            bill.UpdatedAt = DateTime.UtcNow;

            // Cập nhật thông tin thanh toán nếu cần
            // bill.PaymentMethod = paymentMethod;
            // bill.TransactionId = transactionId;

            // Nếu là bill của tour, tăng CurrentBookings cho schedule tương ứng
            var tourDetail = bill.BillDetails
                .FirstOrDefault(bd => bd.ItemType == TableType.Tour && bd.ScheduleID.HasValue);
            if (tourDetail != null)
            {
                var schedule = await _context.Schedules
                    .FirstOrDefaultAsync(s => s.ScheduleID == tourDetail.ScheduleID!.Value);
                if (schedule != null)
                {
                    // Bảo vệ không vượt capacity
                    var remaining = schedule.NumberPeople - schedule.CurrentBookings;
                    var increment = Math.Min(remaining, tourDetail.Quantity);
                    if (increment > 0)
                    {
                        schedule.CurrentBookings += increment;
                    }
                }
            }

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
                TargetID = bill.
                UserName = bill.User.FullName,
                BookingID = bill.BookingID,
                StartTime = booking.CheckInDate,
                EndTime = booking.CheckOutDate,
                Deposit = bill.Deposit,
                TotalPrice = bill.TotalPrice,
                Status = bill.Status.ToString(),
                CreatedAt = bill.CreatedAt,
                User = new UserInfoDTO
                {
                    UserID = bill.User.UserID,
                    FullName = bill.User.FullName,
                    Email = bill.User.Email,
                    PhoneNumber = bill.User.PhoneNumber,
                    CitizenId = bill.User.CitizenId
                },
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
                }).ToList() ?? new List<BillDetailResponseDTO>()
            };
        }
    }
}
