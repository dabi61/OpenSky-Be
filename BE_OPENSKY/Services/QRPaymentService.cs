using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services
{
    public class QRPaymentService : IQRPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBillService _billService;
        private readonly IBookingService _bookingService;

        // Dictionary để lưu trữ QR codes tạm thời (trong thực tế dùng Redis)
        private static readonly Dictionary<string, QRPaymentData> _qrPayments = new();

        public QRPaymentService(ApplicationDbContext context, IBillService billService, IBookingService bookingService)
        {
            _context = context;
            _billService = billService;
            _bookingService = bookingService;
        }

        public async Task<QRPaymentResponseDTO> CreateQRPaymentAsync(QRPaymentRequestDTO request)
        {
            // Kiểm tra Bill có tồn tại không (sử dụng context trực tiếp)
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.BillID == request.BillId);
            if (bill == null)
                throw new ArgumentException("Không tìm thấy hóa đơn");

            // Tạo QR code đơn giản (trong thực tế dùng thư viện QR code)
            var qrCode = $"QR_PAY_{request.BillId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var paymentUrl = $"https://localhost:7006/api/payments/qr/scan?code={qrCode}";
            var expiresAt = DateTime.UtcNow.AddMinutes(15); // QR code hết hạn sau 15 phút

            // Lưu thông tin QR payment
            _qrPayments[qrCode] = new QRPaymentData
            {
                BillId = request.BillId,
                Amount = bill.TotalPrice,
                OrderDescription = $"Thanh toán hóa đơn #{request.BillId}",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                Status = "Pending"
            };

            return new QRPaymentResponseDTO
            {
                QRCode = qrCode,
                PaymentUrl = paymentUrl,
                BillId = request.BillId,
                Amount = bill.TotalPrice,
                OrderDescription = $"Thanh toán hóa đơn #{request.BillId}",
                ExpiresAt = expiresAt
            };
        }

        public async Task<QRPaymentStatusDTO> ScanQRPaymentAsync(string qrCode)
        {
            // Kiểm tra QR code có tồn tại không
            if (!_qrPayments.TryGetValue(qrCode, out var qrPayment))
            {
                return new QRPaymentStatusDTO
                {
                    Status = "Expired",
                    Message = "QR code không hợp lệ hoặc đã hết hạn"
                };
            }

            // Kiểm tra QR code có hết hạn không
            if (DateTime.UtcNow > qrPayment.ExpiresAt)
            {
                _qrPayments.Remove(qrCode);
                return new QRPaymentStatusDTO
                {
                    Status = "Expired",
                    Message = "QR code đã hết hạn"
                };
            }

            // Kiểm tra đã thanh toán chưa
            if (qrPayment.Status == "Paid")
            {
                return new QRPaymentStatusDTO
                {
                    Status = "Paid",
                    Message = "Đã thanh toán thành công",
                    PaidAt = qrPayment.PaidAt
                };
            }

            // Mô phỏng thanh toán thành công (trong thực tế sẽ có xác thực)
            qrPayment.Status = "Paid";
            qrPayment.PaidAt = DateTime.UtcNow;

            // Cập nhật Bill status
            await _billService.UpdateBillPaymentStatusAsync(qrPayment.BillId, "Paid", "QR_PAYMENT", qrPayment.Amount);

            // Cập nhật Booking payment status và status
            await _bookingService.UpdateBookingPaymentStatusAsync(qrPayment.BillId, "Paid");
            
            // Cập nhật Booking status thành Completed
            await _bookingService.UpdateBookingStatusAsync(qrPayment.BillId, "Completed");

            return new QRPaymentStatusDTO
            {
                Status = "Paid",
                Message = "Thanh toán thành công!",
                PaidAt = qrPayment.PaidAt
            };
        }

        public async Task<QRPaymentStatusDTO> GetPaymentStatusAsync(Guid billId)
        {
            // Tìm QR payment theo BillId
            var qrPayment = _qrPayments.Values.FirstOrDefault(x => x.BillId == billId);
            if (qrPayment == null)
            {
                return new QRPaymentStatusDTO
                {
                    Status = "Expired",
                    Message = "Không tìm thấy thông tin thanh toán"
                };
            }

            return new QRPaymentStatusDTO
            {
                Status = qrPayment.Status,
                Message = qrPayment.Status == "Paid" ? "Đã thanh toán thành công" : "Chờ thanh toán",
                PaidAt = qrPayment.PaidAt
            };
        }

        // Class helper để lưu trữ thông tin QR payment
        private class QRPaymentData
        {
            public Guid BillId { get; set; }
            public decimal Amount { get; set; }
            public string OrderDescription { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public string Status { get; set; } = "Pending";
            public DateTime? PaidAt { get; set; }
        }
    }
}
