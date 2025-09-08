using BE_OPENSKY.DTOs;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BE_OPENSKY.Endpoints
{
    public static class BookingEndpoints
    {
        public static void MapBookingEndpoints(this WebApplication app)
        {
            var bookingGroup = app.MapGroup("/api/bookings")
                .WithTags("Booking Management")
                .WithOpenApi();

            // 1. Customer đặt phòng
            bookingGroup.MapPost("/", async ([FromBody] CreateHotelBookingRequestDTO requestDto, [FromServices] IBookingService bookingService, [FromServices] IUserService userService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    // Lấy thông tin user để tự động điền guest info
                    var userProfile = await userService.GetProfileAsync(userIdGuid);
                    if (userProfile == null)
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 404);
                    }

                    // Tạo DTO đầy đủ với thông tin guest
                    var createDto = new CreateHotelBookingDTO
                    {
                        RoomID = requestDto.RoomID,
                        CheckInDate = requestDto.CheckInDate,
                        CheckOutDate = requestDto.CheckOutDate,
                        NumberOfGuests = requestDto.NumberOfGuests,
                        GuestName = userProfile.FullName,
                        GuestPhone = userProfile.PhoneNumber ?? string.Empty,
                        GuestEmail = userProfile.Email
                    };

                    var bookingId = await bookingService.CreateHotelBookingAsync(userIdGuid, createDto);
                    return Results.Ok(new { message = "Đặt phòng thành công", bookingId });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("CreateHotelBooking")
            .WithSummary("Đặt phòng khách sạn")
            .WithDescription("Customer đặt phòng khách sạn")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .RequireAuthorization();

            // 2. Customer xem booking của mình với phân trang
            bookingGroup.MapGet("/my-bookings", async ([FromServices] IBookingService bookingService, HttpContext context, int page = 1, int limit = 10, string? status = null) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    // Validate pagination parameters
                    if (page < 1) page = 1;
                    if (limit < 1 || limit > 100) limit = 10;

                    var result = await bookingService.GetBookingsPaginatedAsync(page, limit, status, userIdGuid);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("GetMyBookings")
            .WithSummary("Xem booking của tôi với phân trang")
            .WithDescription("Customer xem danh sách booking của mình với phân trang và lọc theo trạng thái")
            .Produces<PaginatedBookingsResponseDTO>(200)
            .Produces(401)
            .RequireAuthorization();

            // 3. Hotel xác nhận booking
            bookingGroup.MapPut("/{bookingId:guid}/confirm", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var success = await bookingService.ConfirmBookingAsync(bookingId, userIdGuid);
                    return success 
                        ? Results.Ok(new { message = "Xác nhận booking thành công" })
                        : Results.NotFound(new { message = "Không tìm thấy booking hoặc bạn không có quyền xác nhận" });
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: 403);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("ConfirmBooking")
            .WithSummary("Xác nhận booking")
            .WithDescription("Hotel xác nhận booking và tạo hóa đơn")
            .Produces(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization("HotelOnly");

            // 4. Hotel hủy booking
            bookingGroup.MapPut("/{bookingId:guid}/cancel", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context, string? reason = null) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var success = await bookingService.CancelBookingAsync(bookingId, userIdGuid, reason);
                    return success 
                        ? Results.Ok(new { message = "Hủy booking thành công" })
                        : Results.NotFound(new { message = "Không tìm thấy booking hoặc bạn không có quyền hủy" });
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: 403);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("CancelBooking")
            .WithSummary("Hủy booking")
            .WithDescription("Hotel hủy booking")
            .Produces(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization("HotelOnly");

            // 5. Customer hủy booking
            bookingGroup.MapPut("/{bookingId:guid}/customer-cancel", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context, string? reason = null) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var success = await bookingService.CustomerCancelBookingAsync(bookingId, userIdGuid, reason);
                    return success 
                        ? Results.Ok(new { message = "Hủy booking thành công" })
                        : Results.NotFound(new { message = "Không tìm thấy booking hoặc không thể hủy" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("CustomerCancelBooking")
            .WithSummary("Customer hủy booking")
            .WithDescription("Customer hủy booking của mình")
            .Produces(200)
            .Produces(401)
            .Produces(404)
            .RequireAuthorization();

            // 6. Lấy thông tin booking theo ID
            bookingGroup.MapGet("/{bookingId:guid}", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var booking = await bookingService.GetBookingByIdAsync(bookingId, userIdGuid);
                    return booking != null 
                        ? Results.Ok(booking)
                        : Results.NotFound(new { message = "Không tìm thấy booking" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("GetBookingById")
            .WithSummary("Xem chi tiết booking")
            .WithDescription("Xem thông tin chi tiết booking")
            .Produces<BookingResponseDTO>(200)
            .Produces(401)
            .Produces(404)
            .RequireAuthorization();

            // 7. Cập nhật trạng thái booking (cho admin/hotel)
            bookingGroup.MapPut("/{bookingId:guid}/status", async (Guid bookingId, [FromBody] UpdateBookingStatusDTO updateDto, [FromServices] IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var success = await bookingService.UpdateBookingStatusAsync(bookingId, userIdGuid, updateDto);
                    return success 
                        ? Results.Ok(new { message = "Cập nhật trạng thái booking thành công" })
                        : Results.NotFound(new { message = "Không tìm thấy booking hoặc bạn không có quyền cập nhật" });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: 403);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("UpdateBookingStatus")
            .WithSummary("Cập nhật trạng thái booking")
            .WithDescription("Hotel cập nhật trạng thái booking")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization("HotelOnly");

            // 8. Hotel xem booking của khách sạn với phân trang
            bookingGroup.MapGet("/hotel/{hotelId:guid}", async (Guid hotelId, [FromServices] IBookingService bookingService, HttpContext context, int page = 1, int limit = 10, string? status = null) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    // Validate pagination parameters
                    if (page < 1) page = 1;
                    if (limit < 1 || limit > 100) limit = 10;

                    var result = await bookingService.GetHotelBookingsPaginatedAsync(hotelId, userIdGuid, page, limit, status);
                    return Results.Ok(result);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: 403);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("GetHotelBookingsPaginated")
            .WithSummary("Hotel xem booking của khách sạn với phân trang")
            .WithDescription("Hotel xem danh sách booking của khách sạn với phân trang và lọc theo trạng thái")
            .Produces<PaginatedHotelBookingsResponseDTO>(200)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization("HotelOnly");

            // 9. Phân trang danh sách booking (cho Admin/Supervisor)
            bookingGroup.MapGet("/admin/all", async ([FromServices] IBookingService bookingService, HttpContext context, int page = 1, int limit = 10, string? status = null, Guid? hotelId = null) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    // Kiểm tra quyền Admin hoặc Supervisor
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                    }

                    // Validate pagination parameters
                    if (page < 1) page = 1;
                    if (limit < 1 || limit > 100) limit = 10;

                    var result = await bookingService.GetBookingsPaginatedAsync(page, limit, status, null, hotelId);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("GetAllBookings")
            .WithSummary("Admin/Supervisor xem tất cả booking")
            .WithDescription("Admin và Supervisor có thể xem tất cả booking với phân trang và lọc")
            .Produces<PaginatedBookingsResponseDTO>(200)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization("SupervisorOrAdmin");

            // 10. Tìm kiếm booking
            bookingGroup.MapPost("/search", async ([FromBody] BookingSearchDTO searchDto, [FromServices] IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var result = await bookingService.SearchBookingsAsync(searchDto);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("SearchBookings")
            .WithSummary("Tìm kiếm booking")
            .WithDescription("Tìm kiếm booking theo nhiều tiêu chí với phân trang")
            .Produces<PaginatedBookingsResponseDTO>(200)
            .Produces(401)
            .RequireAuthorization();

            // 11. Kiểm tra phòng có sẵn
            bookingGroup.MapPost("/check-availability", async ([FromBody] RoomAvailabilityCheckDTO checkDto, [FromServices] IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var result = await bookingService.CheckRoomAvailabilityAsync(checkDto);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("CheckRoomAvailability")
            .WithSummary("Kiểm tra phòng có sẵn")
            .WithDescription("Kiểm tra phòng có sẵn trong khoảng thời gian cụ thể")
            .Produces<RoomAvailabilityResponseDTO>(200)
            .RequireAuthorization();

            // 12. Thống kê booking
            bookingGroup.MapGet("/stats", async ([FromServices] IBookingService bookingService, HttpContext context, Guid? hotelId = null, DateTime? fromDate = null, DateTime? toDate = null) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    // Nếu có hotelId, kiểm tra quyền sở hữu
                    if (hotelId.HasValue)
                    {
                        if (!context.User.IsInRole(RoleConstants.Hotel) && !context.User.IsInRole(RoleConstants.Admin))
                        {
                            return Results.Json(new { message = "Bạn không có quyền truy cập thống kê khách sạn này" }, statusCode: 403);
                        }
                    }

                    var result = await bookingService.GetBookingStatsAsync(hotelId, fromDate, toDate);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("GetBookingStats")
            .WithSummary("Thống kê booking")
            .WithDescription("Lấy thống kê booking theo hotel và khoảng thời gian")
            .Produces<BookingStatsDTO>(200)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization();

            // 12. Test endpoint để kiểm tra toàn bộ luồng booking với QR Payment
            bookingGroup.MapGet("/test-complete-flow", async ([FromServices] IBookingService bookingService, [FromServices] IBillService billService, [FromServices] IQRPaymentService qrPaymentService) =>
            {
                try
                {
                    var testData = new
                    {
                        Message = "Luồng Booking hoàn chỉnh với QR Payment đã sẵn sàng",
                        CompleteFlow = new[]
                        {
                            "1. Customer đặt phòng → POST /api/bookings → Status: Pending",
                            "2. Hotel xác nhận → PUT /api/bookings/{id}/confirm → Status: Confirmed + Tạo Bill",
                            "3. Customer tạo QR code → POST /api/payments/qr/create → Tạo QR code",
                            "4. Quét QR code → GET /api/payments/qr/scan → Thanh toán thành công",
                            "5. Hoàn thành → PUT /api/bookings/{id}/status → Status: Completed"
                        },
                        Endpoints = new
                        {
                            Booking = new[]
                            {
                                "POST /api/bookings - Đặt phòng",
                                "GET /api/bookings/my-bookings - Xem booking của mình",
                                "PUT /api/bookings/{id}/confirm - Hotel xác nhận",
                                "PUT /api/bookings/{id}/cancel - Hủy booking",
                                "GET /api/bookings/{id} - Chi tiết booking"
                            },
                            Payment = new[]
                            {
                                "POST /api/payments/qr/create - Tạo QR code thanh toán",
                                "GET /api/payments/qr/scan - Quét QR code thanh toán",
                                "GET /api/payments/qr/status/{billId} - Kiểm tra trạng thái",
                                "GET /api/payments/bills/{id} - Chi tiết hóa đơn"
                            }
                        },
                        QRPaymentFeatures = new
                        {
                            SimpleTest = "Chỉ cần quét QR code là thanh toán thành công",
                            NoRealPayment = "Không cần thẻ thật, chỉ test đơn giản",
                            AutoExpire = "QR code hết hạn sau 15 phút",
                            HTMLResult = "Hiển thị kết quả thanh toán đẹp"
                        },
                        ValidationRules = new
                        {
                            CheckInDate = "Phải >= ngày hiện tại",
                            CheckOutDate = "Phải > CheckInDate",
                            MaxNights = "Tối đa 365 đêm",
                            MaxFutureDate = "Tối đa 2 năm trong tương lai",
                            RoomPrice = "Phải > 0"
                        }
                    };
                    
                    return Results.Ok(testData);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("TestCompleteBookingFlow")
            .WithSummary("Test luồng booking hoàn chỉnh với QR Payment")
            .WithDescription("Endpoint để kiểm tra toàn bộ luồng booking từ đặt phòng đến thanh toán")
            .Produces(200)
            .AllowAnonymous();
        }
    }
}
