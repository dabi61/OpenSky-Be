namespace BE_OPENSKY.Endpoints;

public static class HotelReviewEndpoints
{
    public static void MapHotelReviewEndpoints(this WebApplication app)
    {
        var reviewGroup = app.MapGroup("/feedback")
            .WithTags("Hotel Feedback")
            .WithOpenApi();

        // 1. Tạo đánh giá Hotel
        reviewGroup.MapPost("/", async ([FromBody] CreateHotelReviewDTO reviewDto, [FromServices] IHotelReviewService reviewService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                var review = await reviewService.CreateHotelReviewAsync(reviewDto.HotelId, userId, reviewDto);
                
                return review != null 
                    ? Results.Ok(review)
                    : Results.BadRequest(new { message = "Không thể tạo đánh giá. Có thể bạn đã đánh giá rồi hoặc khách sạn không tồn tại." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi tạo đánh giá: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("CreateHotelFeedback")
        .WithSummary("Đánh giá khách sạn")
        .WithDescription("Tạo đánh giá cho khách sạn (1-5 sao). Chỉ có thể đánh giá sau khi đã đặt phòng và thanh toán thành công. Request body cần có hotelId, rate, description.")
        .Produces<HotelReviewResponseDTO>(200)
        .Produces(400)
        .Produces(401)
        .RequireAuthorization("AuthenticatedOnly");

        // 2. Kiểm tra user có thể đánh giá hotel không
        reviewGroup.MapGet("/eligibility", async ([FromQuery] Guid hotelId, [FromServices] IHotelReviewService reviewService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                var eligibility = await reviewService.CheckReviewEligibilityAsync(hotelId, userId);
                return Results.Ok(eligibility);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi kiểm tra điều kiện đánh giá: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("CheckFeedbackEligibility")
        .WithSummary("Kiểm tra điều kiện đánh giá")
        .WithDescription("Kiểm tra xem user có thể đánh giá hotel hay không (đã đặt phòng và thanh toán)")
        .Produces<ReviewEligibilityDTO>(200)
        .Produces(401)
        .RequireAuthorization("AuthenticatedOnly");

        // 3. Cập nhật đánh giá Hotel
        reviewGroup.MapPut("/{reviewId:guid}", async (Guid reviewId, [FromBody] UpdateHotelReviewDTO updateDto, [FromServices] IHotelReviewService reviewService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                var review = await reviewService.UpdateHotelReviewAsync(reviewId, userId, updateDto);
                
                return review != null 
                    ? Results.Ok(review)
                    : Results.NotFound(new { message = "Không tìm thấy đánh giá hoặc bạn không có quyền cập nhật." });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi cập nhật đánh giá: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateHotelFeedback")
        .WithSummary("Cập nhật đánh giá khách sạn")
        .WithDescription("Cập nhật đánh giá khách sạn của bạn. Request body cần có hotelId, rate, description.")
        .Produces<HotelReviewResponseDTO>(200)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization("AuthenticatedOnly");

        // 3. Xóa đánh giá Hotel
        reviewGroup.MapDelete("/{reviewId:guid}", async (Guid hotelId, Guid reviewId, [FromServices] IHotelReviewService reviewService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                var success = await reviewService.DeleteHotelReviewAsync(reviewId, userId);
                
                return success 
                    ? Results.Ok(new { message = "Xóa đánh giá thành công" })
                    : Results.NotFound(new { message = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa." });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi xóa đánh giá: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("DeleteHotelFeedback")
        .WithSummary("Xóa đánh giá khách sạn")
        .WithDescription("Xóa đánh giá khách sạn của bạn")
        .Produces(200)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization("AuthenticatedOnly");

        // 4. Xem đánh giá Hotel theo ID
        reviewGroup.MapGet("/{reviewId:guid}", async (Guid hotelId, Guid reviewId, [FromServices] IHotelReviewService reviewService) =>
        {
            try
            {
                var review = await reviewService.GetHotelReviewByIdAsync(reviewId);
                
                return review != null 
                    ? Results.Ok(review)
                    : Results.NotFound(new { message = "Không tìm thấy đánh giá" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy đánh giá: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelFeedbackById")
        .WithSummary("Xem đánh giá khách sạn theo ID")
        .WithDescription("Lấy thông tin chi tiết của một đánh giá khách sạn")
        .Produces<HotelReviewResponseDTO>(200)
        .Produces(404);

        // 5. Xem danh sách đánh giá Hotel
        reviewGroup.MapGet("/hotel", async ([FromQuery] Guid hotelId, [FromServices] IHotelReviewService reviewService, int page = 1, int limit = 10) =>
        {
            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                var result = await reviewService.GetHotelReviewsAsync(hotelId, page, limit);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy đánh giá khách sạn: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelFeedbacks")
        .WithSummary("Xem đánh giá khách sạn")
        .WithDescription("Lấy danh sách đánh giá của khách sạn với phân trang")
        .Produces<PaginatedHotelReviewsResponseDTO>(200);

        // 6. Thống kê đánh giá Hotel
        reviewGroup.MapGet("/hotel/stats", async ([FromQuery] Guid hotelId, [FromServices] IHotelReviewService reviewService) =>
        {
            try
            {
                var stats = await reviewService.GetHotelReviewStatsAsync(hotelId);
                return Results.Ok(stats);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy thống kê đánh giá: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelFeedbackStats")
        .WithSummary("Thống kê đánh giá khách sạn")
        .WithDescription("Lấy thống kê đánh giá của khách sạn (điểm trung bình, số lượng đánh giá)")
        .Produces<HotelReviewStatsDTO>(200);

        // 7. Xem đánh giá Hotel của user hiện tại
        reviewGroup.MapGet("/my-feedbacks", async ([FromServices] IHotelReviewService reviewService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                var reviews = await reviewService.GetUserHotelReviewsAsync(userId);
                return Results.Ok(reviews);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy đánh giá của bạn: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetMyHotelFeedbacks")
        .WithSummary("Xem những đánh giá khách sạn mà tôi đã đánh giá")
        .WithDescription("Lấy danh sách đánh giá khách sạn của user hiện tại")
        .Produces<List<HotelReviewResponseDTO>>(200)
        .Produces(401)
        .RequireAuthorization("AuthenticatedOnly");
    }
}
