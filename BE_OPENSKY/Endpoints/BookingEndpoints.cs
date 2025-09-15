using Microsoft.AspNetCore.Authorization;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Services;
using BE_OPENSKY.Helpers;

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
            bookingGroup.MapPost("/hotel", async ([FromBody] CreateMultipleRoomBookingRequestDTO requestDto, [FromServices] IBookingService bookingService, HttpContext context) =>
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
            bookingGroup.MapGet("/hotel/{bookingId:guid}/detail", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context) =>
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

            // 3. Customer xem booking của mình với phân trang (Hotel + Tour)
            bookingGroup.MapGet("/my-bookings", async (
                [FromServices] IBookingService bookingService,
                [FromServices] ITourBookingService tourBookingService,
                HttpContext context, 
                int page = 1, 
                int limit = 10, 
                string? status = null,
                string? type = null) => // type: "hotel", "tour", hoặc null (tất cả)
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

                    var result = new
                    {
                        HotelBookings = new List<object>(),
                        TourBookings = new List<object>(),
                        TotalCount = 0,
                        Page = page,
                        Limit = limit,
                        TotalPages = 0
                    };

                    // Lấy hotel bookings nếu type là "hotel" hoặc null
                    if (type == null || type.ToLower() == "hotel")
                    {
                        var hotelResult = await bookingService.GetBookingsPaginatedAsync(page, limit, status, userIdGuid);
                        result = new
                        {
                            HotelBookings = hotelResult.Bookings?.Cast<object>().ToList() ?? new List<object>(),
                            TourBookings = new List<object>(),
                            TotalCount = hotelResult.TotalBookings,
                            Page = hotelResult.CurrentPage,
                            Limit = hotelResult.PageSize,
                            TotalPages = hotelResult.TotalPages
                        };
                    }

                    // Lấy tour bookings nếu type là "tour" hoặc null
                    if (type == null || type.ToLower() == "tour")
                    {
                        var tourResult = await tourBookingService.GetUserTourBookingsAsync(userIdGuid, page, limit);
                        var currentHotelBookings = type == null ? result.HotelBookings : new List<object>();
                        var currentTotalCount = type == null ? result.TotalCount + tourResult.TotalCount : tourResult.TotalCount;
                        
                        result = new
                        {
                            HotelBookings = currentHotelBookings,
                            TourBookings = tourResult.Bookings?.Cast<object>().ToList() ?? new List<object>(),
                            TotalCount = currentTotalCount,
                            Page = tourResult.Page,
                            Limit = tourResult.Size,
                            TotalPages = tourResult.TotalPages
                        };
                    }

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
            .WithDescription("Customer xem danh sách booking của mình (Hotel + Tour) với phân trang và lọc theo trạng thái/loại")
            .Produces<object>(200)
            .Produces(401)
            .RequireAuthorization();

            // Bỏ bước hotel confirm/hủy vì auto-approve

            // 5. Customer hủy booking
            bookingGroup.MapPut("/hotel/{bookingId:guid}/customer-cancel", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context, string? reason = null) =>
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

            // 10. Tìm kiếm booking (Hotel + Tour)
            bookingGroup.MapGet("/search", async (
                [FromServices] IBookingService bookingService,
                [FromServices] ITourBookingService tourBookingService,
                HttpContext context, 
                [FromQuery] string? query = null, 
                [FromQuery] string? status = null, 
                [FromQuery] DateTime? fromDate = null, 
                [FromQuery] DateTime? toDate = null, 
                [FromQuery] Guid? hotelId = null,
                [FromQuery] Guid? tourId = null,
                [FromQuery] string? type = null, // "hotel", "tour", hoặc null (tất cả)
                [FromQuery] int page = 1, 
                [FromQuery] int limit = 10) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var result = new
                    {
                        HotelBookings = new List<object>(),
                        TourBookings = new List<object>(),
                        TotalCount = 0,
                        Page = page,
                        Limit = limit,
                        TotalPages = 0
                    };

                    // Tìm kiếm hotel bookings nếu type là "hotel" hoặc null
                    if (type == null || type.ToLower() == "hotel")
                    {
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

                        var hotelResult = await bookingService.SearchBookingsAsync(searchDto);
                        result = new
                        {
                            HotelBookings = hotelResult.Bookings?.Cast<object>().ToList() ?? new List<object>(),
                            TourBookings = new List<object>(),
                            TotalCount = hotelResult.TotalBookings,
                            Page = hotelResult.CurrentPage,
                            Limit = hotelResult.PageSize,
                            TotalPages = hotelResult.TotalPages
                        };
                    }

                    // Tìm kiếm tour bookings nếu type là "tour" hoặc null
                    if (type == null || type.ToLower() == "tour")
                    {
                        var tourResult = await tourBookingService.GetUserTourBookingsAsync(userIdGuid, page, limit);
                        
                        // Filter theo tourId nếu có
                        var filteredTours = tourResult.Bookings;
                        if (tourId.HasValue)
                        {
                            filteredTours = tourResult.Bookings.Where(t => t.TourID == tourId.Value).ToList();
                        }

                        var currentHotelBookings = type == null ? result.HotelBookings : new List<object>();
                        var currentTotalCount = type == null ? result.TotalCount + filteredTours.Count : filteredTours.Count;
                        
                        result = new
                        {
                            HotelBookings = currentHotelBookings,
                            TourBookings = filteredTours?.Cast<object>().ToList() ?? new List<object>(),
                            TotalCount = currentTotalCount,
                            Page = tourResult.Page,
                            Limit = tourResult.Size,
                            TotalPages = tourResult.TotalPages
                        };
                    }

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
            .WithDescription("Tìm kiếm booking (Hotel + Tour) theo nhiều tiêu chí với phân trang")
            .Produces<object>(200)
            .Produces(401)
            .RequireAuthorization();

            // 11. Kiểm tra phòng có sẵn
            bookingGroup.MapPost("/hotel/check-availability", async ([FromBody] RoomAvailabilityCheckDTO checkDto, [FromServices] IBookingService bookingService, HttpContext context) =>
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
            bookingGroup.MapPut("/hotel/{bookingId:guid}/check-in", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context) =>
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
            bookingGroup.MapPut("/hotel/{bookingId:guid}/check-out", async (Guid bookingId, [FromServices] IBookingService bookingService, HttpContext context) =>
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

            // 14. Thống kê booking (Hotel + Tour)
            bookingGroup.MapGet("/stats", async (
                [FromServices] IBookingService bookingService,
                [FromServices] ITourBookingService tourBookingService,
                HttpContext context, 
                Guid? hotelId = null, 
                Guid? tourId = null,
                DateTime? fromDate = null, 
                DateTime? toDate = null,
                string? type = null) => // "hotel", "tour", hoặc null (tất cả)
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    dynamic result = new
                    {
                        HotelStats = new object(),
                        TourStats = new object(),
                        TotalStats = new
                        {
                            TotalBookings = 0,
                            TotalRevenue = 0m,
                            PendingBookings = 0,
                            ConfirmedBookings = 0,
                            CancelledBookings = 0
                        }
                    };

                    // Lấy thống kê hotel nếu type là "hotel" hoặc null
                    if (type == null || type.ToLower() == "hotel")
                    {
                        // Nếu có hotelId, kiểm tra quyền sở hữu
                        if (hotelId.HasValue)
                        {
                            if (!context.User.IsInRole(RoleConstants.Hotel) && !context.User.IsInRole(RoleConstants.Admin))
                            {
                                return Results.Json(new { message = "Bạn không có quyền truy cập thống kê khách sạn này" }, statusCode: 403);
                            }
                        }

                        var hotelStats = await bookingService.GetBookingStatsAsync(hotelId, fromDate, toDate);
                        var hotelStatsObj = new
                        {
                            TotalBookings = hotelStats.TotalBookings,
                            TotalRevenue = hotelStats.TotalRevenue,
                            PendingBookings = hotelStats.PendingBookings,
                            ConfirmedBookings = hotelStats.ConfirmedBookings,
                            CancelledBookings = hotelStats.CancelledBookings
                        };
                        
                        result = new
                        {
                            HotelStats = (object)hotelStatsObj,
                            TourStats = (object)new { },
                            TotalStats = new
                            {
                                TotalBookings = hotelStats.TotalBookings,
                                TotalRevenue = hotelStats.TotalRevenue,
                                PendingBookings = hotelStats.PendingBookings,
                                ConfirmedBookings = hotelStats.ConfirmedBookings,
                                CancelledBookings = hotelStats.CancelledBookings
                            }
                        };
                    }

                    // Lấy thống kê tour nếu type là "tour" hoặc null
                    if (type == null || type.ToLower() == "tour")
                    {
                        var tourResult = await tourBookingService.GetUserTourBookingsAsync(userIdGuid, 1, int.MaxValue);
                        
                        // Filter theo tourId nếu có
                        var filteredTours = tourResult.Bookings;
                        if (tourId.HasValue)
                        {
                            filteredTours = tourResult.Bookings.Where(t => t.TourID == tourId.Value).ToList();
                        }

                        var tourStats = new
                        {
                            TotalBookings = filteredTours.Count,
                            PendingBookings = filteredTours.Count(t => t.Status == "Pending"),
                            ConfirmedBookings = filteredTours.Count(t => t.Status == "Confirmed"),
                            CancelledBookings = filteredTours.Count(t => t.Status == "Cancelled"),
                            TotalRevenue = filteredTours.Sum(t => t.TourInfo?.Price ?? 0)
                        };

                        var currentHotelStats = type == null ? result.HotelStats : new object();
                        var currentTotalBookings = type == null ? ((dynamic)result.TotalStats).TotalBookings + tourStats.TotalBookings : tourStats.TotalBookings;
                        var currentTotalRevenue = type == null ? ((dynamic)result.TotalStats).TotalRevenue + tourStats.TotalRevenue : tourStats.TotalRevenue;
                        var currentPendingBookings = type == null ? ((dynamic)result.TotalStats).PendingBookings + tourStats.PendingBookings : tourStats.PendingBookings;
                        var currentConfirmedBookings = type == null ? ((dynamic)result.TotalStats).ConfirmedBookings + tourStats.ConfirmedBookings : tourStats.ConfirmedBookings;
                        var currentCancelledBookings = type == null ? ((dynamic)result.TotalStats).CancelledBookings + tourStats.CancelledBookings : tourStats.CancelledBookings;

                        result = new
                        {
                            HotelStats = (object)currentHotelStats,
                            TourStats = (object)tourStats,
                            TotalStats = new
                            {
                                TotalBookings = currentTotalBookings,
                                TotalRevenue = currentTotalRevenue,
                                PendingBookings = currentPendingBookings,
                                ConfirmedBookings = currentConfirmedBookings,
                                CancelledBookings = currentCancelledBookings
                            }
                        };
                    }

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
            .WithDescription("Lấy thống kê booking (Hotel + Tour) theo hotel/tour và khoảng thời gian")
            .Produces<object>(200)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization();

            // ========== TOUR BOOKING ENDPOINTS ==========

            // 15. Tạo tour booking
            bookingGroup.MapPost("/tour", async (
                CreateTourBookingDTO createBookingDto,
                ITourBookingService tourBookingService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var bookingId = await tourBookingService.CreateTourBookingAsync(userId, createBookingDto);
                    return Results.Created($"/bookings/{bookingId}", new { BookingID = bookingId, Message = "Tạo tour booking thành công" });
                }
                catch (ArgumentException ex)
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
            .WithName("CreateTourBooking")
            .WithSummary("Tạo tour booking")
            .WithDescription("Tạo booking mới cho tour")
            .Produces<object>(201)
            .Produces(400)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // 16. Lấy tour booking theo ID
            bookingGroup.MapGet("/tour/{bookingId}", async (
                Guid bookingId,
                ITourBookingService tourBookingService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var booking = await tourBookingService.GetTourBookingByIdAsync(bookingId, userId);
                    if (booking == null)
                        return Results.NotFound(new { message = "Không tìm thấy tour booking" });

                    return Results.Ok(booking);
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
            .WithName("GetTourBookingById")
            .WithSummary("Lấy tour booking theo ID")
            .WithDescription("Lấy chi tiết tour booking")
            .Produces<TourBookingResponseDTO>(200)
            .Produces(401)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization();


            // 18. Cập nhật tour booking
            bookingGroup.MapPut("/tour/{bookingId}", async (
                Guid bookingId,
                UpdateTourBookingDTO updateBookingDto,
                ITourBookingService tourBookingService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var success = await tourBookingService.UpdateTourBookingAsync(bookingId, userId, updateBookingDto);
                    if (!success)
                        return Results.NotFound(new { message = "Không tìm thấy tour booking" });

                    return Results.Ok(new { message = "Cập nhật tour booking thành công" });
                }
                catch (ArgumentException ex)
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
            .WithName("UpdateTourBooking")
            .WithSummary("Cập nhật tour booking")
            .WithDescription("Cập nhật thông tin tour booking")
            .Produces<object>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization();

            // 19. Hủy tour booking
            bookingGroup.MapDelete("/tour/{bookingId}", async (
                Guid bookingId,
                ITourBookingService tourBookingService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var success = await tourBookingService.CancelTourBookingAsync(bookingId, userId);
                    if (!success)
                        return Results.NotFound(new { message = "Không tìm thấy tour booking" });

                    return Results.Ok(new { message = "Hủy tour booking thành công" });
                }
                catch (ArgumentException ex)
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
            .WithName("CancelTourBooking")
            .WithSummary("Hủy tour booking")
            .WithDescription("Hủy tour booking")
            .Produces<object>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization();

        }
    }
}
