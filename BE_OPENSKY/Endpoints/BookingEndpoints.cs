using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints
{
    public static class BookingEndpoints
    {
        public static void MapBookingEndpoints(this WebApplication app)
        {
            var bookingGroup = app.MapGroup("/bookings")
                .WithTags("Booking")
                .WithOpenApi();

            // 1. Customer đặt phòng (1 hoặc nhiều phòng)
            bookingGroup.MapPost("/", async ([FromBody] CreateMultipleRoomBookingRequestDTO requestDto, [FromServices] IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    // Tạo DTO đầy đủ
                    var createDto = new CreateMultipleRoomBookingDTO
                    {
                        Rooms = requestDto.Rooms,
                        CheckInDate = requestDto.CheckInDate.Date, // Chỉ lấy ngày, bỏ thời gian
                        CheckOutDate = requestDto.CheckOutDate.Date, // Chỉ lấy ngày, bỏ thời gian
                    };

                    var bookingId = await bookingService.CreateMultipleRoomBookingAsync(userIdGuid, createDto);
                    var totalRooms = requestDto.Rooms.Count; // Số RoomID = số phòng
                    var message = totalRooms == 1 ? "Đặt phòng thành công" : $"Đặt {totalRooms} phòng thành công";
                    
                    return Results.Ok(new { 
                        message = message, 
                        bookingId,
                        totalRooms = totalRooms
                    });
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
            .WithDescription("Customer đặt phòng khách sạn (hỗ trợ cả 1 phòng và nhiều phòng cùng lúc)")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .RequireAuthorization();

            // 2. Customer xem chi tiết booking với BillDetail
            bookingGroup.MapGet("/{bookingId:guid}/detail", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var booking = await bookingService.GetBookingDetailByIdAsync(bookingId, userIdGuid);
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
            .WithName("GetBookingDetail")
            .WithSummary("Xem chi tiết booking với BillDetail")
            .WithDescription("Xem thông tin chi tiết booking bao gồm thông tin phòng từ BillDetail")
            .Produces<BookingDetailResponseDTO>(200)
            .Produces(401)
            .Produces(404)
            .RequireAuthorization();

            // 3. Customer xem booking của mình với phân trang
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

            // Bỏ bước hotel confirm/hủy vì auto-approve

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


            // Bỏ endpoint cập nhật trạng thái thủ công

            // Bỏ endpoint danh sách theo khách sạn

            // Bỏ endpoint admin tổng hợp

            // 10. Tìm kiếm booking
            bookingGroup.MapGet("/search", async ([FromServices] IBookingService bookingService, HttpContext context, [FromQuery] string? query = null, [FromQuery] string? status = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null, [FromQuery] Guid? hotelId = null, [FromQuery] int page = 1, [FromQuery] int limit = 10) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var searchDto = new BookingSearchDTO
                    {
                        Query = query,
                        Status = status,
                        FromDate = fromDate,
                        ToDate = toDate,
                        HotelId = hotelId,
                        Page = page,
                        Limit = limit
                    };

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

            // 12. Check-in booking
            bookingGroup.MapPut("/{bookingId:guid}/check-in", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var success = await bookingService.CheckInBookingAsync(bookingId, userIdGuid);
                    if (success)
                    {
                        return Results.Ok(new { message = "Check-in thành công" });
                    }
                    else
                    {
                        return Results.BadRequest(new { message = "Không thể check-in. Vui lòng kiểm tra trạng thái booking và thanh toán." });
                    }
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
            .WithName("CheckInBooking")
            .WithSummary("Check-in booking")
            .WithDescription("Customer check-in phòng (cập nhật RoomStatus thành Occupied)")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .RequireAuthorization();

            // 13. Check-out booking
            bookingGroup.MapPut("/{bookingId:guid}/check-out", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var success = await bookingService.CheckOutBookingAsync(bookingId, userIdGuid);
                    if (success)
                    {
                        return Results.Ok(new { message = "Check-out thành công" });
                    }
                    else
                    {
                        return Results.BadRequest(new { message = "Không thể check-out. Vui lòng kiểm tra trạng thái booking." });
                    }
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
            .WithName("CheckOutBooking")
            .WithSummary("Check-out booking")
            .WithDescription("Customer check-out phòng (cập nhật RoomStatus thành Available)")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .RequireAuthorization();

            // 14. Thống kê booking
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

        }
    }
}
