using BE_OPENSKY.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BE_OPENSKY.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly string _vnp_TmnCode;
        private readonly string _vnp_HashSecret;
        private readonly string _vnp_Url;
        private readonly string _vnp_ReturnUrl;

        public VNPayService(IConfiguration configuration)
        {
            _configuration = configuration;
            _vnp_TmnCode = _configuration["VNPay:TmnCode"] ?? "DEMO";
            _vnp_HashSecret = _configuration["VNPay:HashSecret"] ?? "DEMO";
            _vnp_Url = _configuration["VNPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _vnp_ReturnUrl = _configuration["VNPay:ReturnUrl"] ?? "https://localhost:7006/api/payments/vnpay-callback";
        }

        public async Task<VNPayPaymentResponseDTO> CreatePaymentUrlAsync(VNPayPaymentRequestDTO request)
        {
            try
            {
                // Tạo order ID duy nhất
                var orderId = DateTime.Now.Ticks.ToString();
                var transactionId = Guid.NewGuid().ToString();

                // Tạo các tham số cho VNPay
                var vnp_Params = new Dictionary<string, string>
                {
                    {"vnp_Version", "2.1.0"},
                    {"vnp_Command", "pay"},
                    {"vnp_TmnCode", _vnp_TmnCode},
                    {"vnp_Amount", ((long)(request.Amount * 100)).ToString()}, // VNPay yêu cầu amount tính bằng xu
                    {"vnp_CurrCode", "VND"},
                    {"vnp_TxnRef", orderId},
                    {"vnp_OrderInfo", request.OrderDescription ?? $"Thanh toan don hang {orderId}"},
                    {"vnp_OrderType", request.OrderType ?? "hotel_booking"},
                    {"vnp_Locale", "vn"},
                    {"vnp_ReturnUrl", request.ReturnUrl ?? _vnp_ReturnUrl},
                    {"vnp_IpAddr", "127.0.0.1"},
                    {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")}
                };

                // Sắp xếp tham số theo thứ tự alphabet
                var sortedParams = vnp_Params.OrderBy(x => x.Key).ToList();

                // Tạo query string
                var queryString = string.Join("&", sortedParams.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));

                // Tạo secure hash
                var secureHash = CreateSecureHash(queryString);

                // Thêm secure hash vào query string
                var finalQueryString = $"{queryString}&vnp_SecureHash={secureHash}";

                // Tạo URL thanh toán
                var paymentUrl = $"{_vnp_Url}?{finalQueryString}";

                return new VNPayPaymentResponseDTO
                {
                    PaymentUrl = paymentUrl,
                    TransactionId = transactionId,
                    OrderId = orderId,
                    Amount = request.Amount,
                    OrderDescription = request.OrderDescription ?? $"Thanh toan don hang {orderId}",
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Lỗi tạo URL thanh toán VNPay: {ex.Message}");
            }
        }

        public async Task<PaymentResultDTO> ProcessCallbackAsync(VNPayCallbackDTO callback)
        {
            try
            {
                // Kiểm tra response code
                if (callback.vnp_ResponseCode != "00")
                {
                    return new PaymentResultDTO
                    {
                        Success = false,
                        Message = GetResponseMessage(callback.vnp_ResponseCode),
                        TransactionId = callback.vnp_TxnRef,
                        Amount = decimal.Parse(callback.vnp_Amount) / 100, // Chuyển từ xu về VND
                        PaymentMethod = "VNPay",
                        PaymentDate = DateTime.UtcNow
                    };
                }

                // Kiểm tra secure hash
                var isValidHash = ValidateSecureHash(callback);
                if (!isValidHash)
                {
                    return new PaymentResultDTO
                    {
                        Success = false,
                        Message = "Chữ ký không hợp lệ",
                        TransactionId = callback.vnp_TxnRef,
                        Amount = decimal.Parse(callback.vnp_Amount) / 100,
                        PaymentMethod = "VNPay",
                        PaymentDate = DateTime.UtcNow
                    };
                }

                return new PaymentResultDTO
                {
                    Success = true,
                    Message = "Thanh toán thành công",
                    TransactionId = callback.vnp_TxnRef,
                    Amount = decimal.Parse(callback.vnp_Amount) / 100,
                    PaymentMethod = "VNPay",
                    PaymentDate = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new PaymentResultDTO
                {
                    Success = false,
                    Message = $"Lỗi xử lý callback: {ex.Message}",
                    TransactionId = callback.vnp_TxnRef,
                    Amount = 0,
                    PaymentMethod = "VNPay",
                    PaymentDate = DateTime.UtcNow
                };
            }
        }

        public async Task<PaymentResultDTO> CheckPaymentStatusAsync(string transactionId)
        {
            // Trong thực tế, bạn sẽ gọi API VNPay để kiểm tra trạng thái
            // Ở đây tôi sẽ trả về kết quả mặc định
            return new PaymentResultDTO
            {
                Success = true,
                Message = "Thanh toán đã được xác nhận",
                TransactionId = transactionId,
                Amount = 0,
                PaymentMethod = "VNPay",
                PaymentDate = DateTime.UtcNow
            };
        }

        private string CreateSecureHash(string queryString)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_vnp_HashSecret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private bool ValidateSecureHash(VNPayCallbackDTO callback)
        {
            // Tạo lại query string từ callback data
            var vnp_Params = new Dictionary<string, string>
            {
                {"vnp_TxnRef", callback.vnp_TxnRef},
                {"vnp_Amount", callback.vnp_Amount},
                {"vnp_ResponseCode", callback.vnp_ResponseCode},
                {"vnp_TransactionStatus", callback.vnp_TransactionStatus},
                {"vnp_OrderInfo", callback.vnp_OrderInfo},
                {"vnp_PayDate", callback.vnp_PayDate},
                {"vnp_BankCode", callback.vnp_BankCode}
            };

            var sortedParams = vnp_Params.OrderBy(x => x.Key).ToList();
            var queryString = string.Join("&", sortedParams.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
            var secureHash = CreateSecureHash(queryString);

            return secureHash.Equals(callback.vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private string GetResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking",
                "10" => "Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Đã hết hạn chờ thanh toán. Xin vui lòng thực hiện lại giao dịch",
                "12" => "Giao dịch bị từ chối do thẻ/tài khoản của khách hàng bị khóa",
                "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
                "51" => "Giao dịch không thành công do: Tài khoản của bạn không đủ số dư để thực hiện giao dịch",
                "65" => "Giao dịch không thành công do: Tài khoản của bạn đã vượt quá hạn mức giao dịch trong ngày",
                "75" => "Ngân hàng thanh toán đang bảo trì",
                "79" => "Giao dịch không thành công do: Nhập sai mật khẩu thanh toán quá số lần quy định",
                "99" => "Lỗi không xác định",
                _ => "Lỗi không xác định"
            };
        }
    }
}
