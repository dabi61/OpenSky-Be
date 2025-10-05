using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BE_OPENSKY.Endpoints
{
    public static class FeedbackEndpoints
    {
        public static void MapFeedbackEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/feedback")
                .WithTags("Feedback")
                .WithOpenApi();

            // POST /feedback - Tạo đánh giá (Hotel hoặc Tour)
            group.MapPost("/", async (
                CreateFeedbackDTO createFeedbackDto,
                IHotelReviewService hotelReviewService,
                ITourReviewService tourReviewService,
                HttpContext context) =>
            {
                try
                {
                    var userId = JwtHelper.GetUserIdFromToken(context.User);
                    if (userId == null)
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    if (createFeedbackDto.Type == "Hotel")
                    {
                        var hotelReview = new CreateHotelReviewDTO
                        {
                            HotelId = createFeedbackDto.TargetId,
                            Rate = createFeedbackDto.Rate,
                            Description = createFeedbackDto.Description
                        };

                        var review = await hotelReviewService.CreateHotelReviewAsync(hotelReview.HotelId, userId.Value, hotelReview);
                        
                        return review != null 
                            ? Results.Json(new { message = "Tạo đánh giá khách sạn thành công", data = review }, statusCode: 201)
                            : Results.BadRequest(new { message = "Không thể tạo đánh giá. Có thể bạn đã đánh giá rồi hoặc khách sạn không tồn tại." });
                    }
                    else if (createFeedbackDto.Type == "Tour")
                    {
                        var tourReview = new CreateTourReviewDTO
                        {
                            TourId = createFeedbackDto.TargetId,
                            Rate = createFeedbackDto.Rate,
                            Description = createFeedbackDto.Description
                        };

                        var reviewId = await tourReviewService.CreateTourReviewAsync(tourReview, userId.Value);

                        return Results.Json(new { 
                            message = "Tạo đánh giá tour thành công", 
                            reviewId = reviewId 
                        }, statusCode: 201);
                    }
                    else
                    {
                        return Results.BadRequest(new { message = "Giá trị 'type' không hợp lệ. Chỉ chấp nhận 'Hotel' hoặc 'Tour'" });
                    }
                }
                catch (InvalidOperationException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi tạo đánh giá: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("CreateFeedback")
            .WithSummary("Tạo đánh giá (Hotel hoặc Tour)")
            .WithDescription("Tạo đánh giá cho khách sạn hoặc tour (1-5 sao). Body phải có: 'type' ('Hotel' hoặc 'Tour'), 'targetId' (ID của hotel/tour), 'rate' (1-5) và 'description'")
            .Produces(201)
            .Produces(400)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // GET /feedback/hotel/{hotelId} - Lấy danh sách đánh giá Hotel
            group.MapGet("/hotel/{hotelId:guid}", async (
                Guid hotelId,
                IHotelReviewService reviewService,
                int page = 1,
                int limit = 10) =>
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
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách đánh giá: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetHotelFeedbacks")
            .WithSummary("Lấy danh sách đánh giá khách sạn")
            .WithDescription("Lấy danh sách đánh giá của khách sạn với phân trang và thống kê")
            .Produces(200)
            .Produces(500);

            // GET /feedback/tour/{tourId} - Lấy danh sách đánh giá Tour
            group.MapGet("/tour/{tourId:guid}", async (
                Guid tourId,
                ITourReviewService reviewService,
                int page = 1,
                int limit = 10) =>
            {
                try
                {
                    if (page < 1) page = 1;
                    if (limit < 1 || limit > 100) limit = 10;

                    var result = await reviewService.GetPaginatedTourReviewsAsync(tourId, page, limit);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách đánh giá: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetTourFeedbacks")
            .WithSummary("Lấy danh sách đánh giá tour")
            .WithDescription("Lấy danh sách đánh giá của tour với phân trang và thống kê")
            .Produces(200)
            .Produces(500);

            // GET /feedback/hotel/{hotelId}/stats - Thống kê đánh giá Hotel
            group.MapGet("/hotel/{hotelId:guid}/stats", async (
                Guid hotelId,
                IHotelReviewService reviewService) =>
            {
                try
                {
                    var stats = await reviewService.GetHotelReviewStatsAsync(hotelId);
                    return Results.Ok(stats);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy thống kê: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetHotelFeedbackStats")
            .WithSummary("Thống kê đánh giá khách sạn")
            .WithDescription("Lấy thống kê đánh giá của khách sạn")
            .Produces(200)
            .Produces(500);

            // GET /feedback/tour/{tourId}/stats - Thống kê đánh giá Tour
            group.MapGet("/tour/{tourId:guid}/stats", async (
                Guid tourId,
                ITourReviewService reviewService) =>
            {
                try
                {
                    var stats = await reviewService.GetTourReviewStatsAsync(tourId);
                    return Results.Ok(stats);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy thống kê: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetTourFeedbackStats")
            .WithSummary("Thống kê đánh giá tour")
            .WithDescription("Lấy thống kê đánh giá của tour")
            .Produces(200)
            .Produces(500);

            // GET /feedback/{targetId}/eligibility - Kiểm tra điều kiện đánh giá (Hotel hoặc Tour)
            group.MapGet("/{targetId:guid}/eligibility", async (
                Guid targetId,
                IHotelReviewService hotelReviewService,
                ITourReviewService tourReviewService,
                HttpContext context,
                string type) =>
            {
                try
                {
                    var userId = JwtHelper.GetUserIdFromToken(context.User);
                    if (userId == null)
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    if (type == "Hotel")
                    {
                        var eligibility = await hotelReviewService.CheckReviewEligibilityAsync(targetId, userId.Value);
                        return Results.Ok(eligibility);
                    }
                    else if (type == "Tour")
                    {
                        var eligibility = await tourReviewService.CheckReviewEligibilityAsync(targetId, userId.Value);
                        return Results.Ok(eligibility);
                    }
                    else
                    {
                        return Results.BadRequest(new { message = "Query parameter 'type' không hợp lệ. Chỉ chấp nhận 'Hotel' hoặc 'Tour'" });
                    }
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("CheckFeedbackEligibility")
            .WithSummary("Kiểm tra điều kiện đánh giá")
            .WithDescription("Kiểm tra xem user có thể đánh giá hay không. Query parameter 'type': 'Hotel' hoặc 'Tour'")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // GET /feedback/my-feedbacks - Lấy tất cả đánh giá của user (cả Hotel và Tour) với phân trang
            group.MapGet("/my-feedbacks", async (
                IHotelReviewService hotelReviewService,
                ITourReviewService tourReviewService,
                HttpContext context,
                int page = 1,
                int limit = 10) =>
            {
                try
                {
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Bạn chưa đăng nhập" }, statusCode: 401);
                    }

                    if (page < 1) page = 1;
                    if (limit < 1 || limit > 100) limit = 10;

                    var hotelReviews = await hotelReviewService.GetUserHotelReviewsAsync(userId);
                    var tourReviews = await tourReviewService.GetUserTourReviewsAsync(userId);

                    // Gộp và sắp xếp theo thời gian tạo
                    var allReviews = new List<object>();
                    allReviews.AddRange(hotelReviews.Select(r => new {
                        type = "Hotel",
                        feedbackId = r.FeedBackID,
                        targetId = r.HotelID,
                        targetName = r.HotelName,
                        rate = r.Rate,
                        description = r.Description,
                        createdAt = r.CreatedAt
                    }));
                    allReviews.AddRange(tourReviews.Select(r => new {
                        type = "Tour",
                        feedbackId = r.FeedBackID,
                        targetId = r.TourID,
                        targetName = r.TourName,
                        rate = r.Rate,
                        description = r.Description,
                        createdAt = r.CreatedAt
                    }));

                    // Sắp xếp theo thời gian tạo (mới nhất trước)
                    var sortedReviews = allReviews.OrderByDescending(r => ((dynamic)r).createdAt).ToList();

                    // Phân trang
                    var totalCount = sortedReviews.Count;
                    var totalPages = (int)Math.Ceiling((double)totalCount / limit);
                    var paginatedReviews = sortedReviews
                        .Skip((page - 1) * limit)
                        .Take(limit)
                        .ToList();

                    return Results.Ok(new {
                        feedbacks = paginatedReviews,
                        totalCount = totalCount,
                        page = page,
                        limit = limit,
                        totalPages = totalPages,
                        hotelCount = hotelReviews.Count,
                        tourCount = tourReviews.Count
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetMyFeedbacks")
            .WithSummary("Lấy tất cả đánh giá của tôi")
            .WithDescription("Lấy danh sách tất cả đánh giá (Hotel + Tour) của user hiện tại với phân trang")
            .Produces(200)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // PUT /feedback/{reviewId} - Cập nhật đánh giá (Hotel hoặc Tour)
            group.MapPut("/{reviewId:guid}", async (
                Guid reviewId,
                UpdateFeedbackDTO updateFeedbackDto,
                IHotelReviewService hotelReviewService,
                ITourReviewService tourReviewService,
                HttpContext context,
                string type) =>
            {
                try
                {
                    var userId = JwtHelper.GetUserIdFromToken(context.User);
                    if (userId == null)
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    if (type == "Hotel")
                    {
                        // Lấy thông tin review để có hotelId
                        var existingReview = await hotelReviewService.GetHotelReviewByIdAsync(reviewId);
                        if (existingReview == null)
                        {
                            return Results.NotFound(new { message = "Không tìm thấy đánh giá" });
                        }

                        var updateDto = new UpdateHotelReviewDTO
                        {
                            HotelId = existingReview.HotelID,
                            Rate = updateFeedbackDto.Rate,
                            Description = updateFeedbackDto.Description
                        };

                        var review = await hotelReviewService.UpdateHotelReviewAsync(reviewId, userId.Value, updateDto);
                        
                        return review != null 
                            ? Results.Ok(new { message = "Cập nhật đánh giá thành công", data = review })
                            : Results.NotFound(new { message = "Không tìm thấy đánh giá hoặc bạn không có quyền cập nhật" });
                    }
                    else if (type == "Tour")
                    {
                        // Lấy thông tin review để có tourId
                        var existingReview = await tourReviewService.GetTourReviewByIdAsync(reviewId);
                        if (existingReview == null)
                        {
                            return Results.NotFound(new { message = "Không tìm thấy đánh giá" });
                        }

                        var updateDto = new UpdateTourReviewDTO
                        {
                            TourId = existingReview.TourID,
                            Rate = updateFeedbackDto.Rate,
                            Description = updateFeedbackDto.Description
                        };

                        var success = await tourReviewService.UpdateTourReviewAsync(reviewId, updateDto, userId.Value);
                        
                        return success 
                            ? Results.Ok(new { message = "Cập nhật đánh giá thành công" })
                            : Results.NotFound(new { message = "Không tìm thấy đánh giá hoặc bạn không có quyền cập nhật" });
                    }
                    else
                    {
                        return Results.BadRequest(new { message = "Query parameter 'type' không hợp lệ. Chỉ chấp nhận 'Hotel' hoặc 'Tour'" });
                    }
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("UpdateFeedback")
            .WithSummary("Cập nhật đánh giá")
            .WithDescription("Cập nhật đánh giá của bạn. Query parameter 'type': 'Hotel' hoặc 'Tour'. Body chỉ cần 'rate' và 'description'")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization();

            // DELETE /feedback/{reviewId} - Xóa đánh giá (Hotel hoặc Tour)
            group.MapDelete("/{reviewId:guid}", async (
                Guid reviewId,
                HttpContext context,
                IHotelReviewService hotelReviewService,
                ITourReviewService tourReviewService,
                string type) =>
            {
                try
                {
                    var userId = JwtHelper.GetUserIdFromToken(context.User);
                    if (userId == null)
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    if (type == "Hotel")
                    {
                        var success = await hotelReviewService.DeleteHotelReviewAsync(reviewId, userId.Value);
                        
                        return success 
                            ? Results.Ok(new { message = "Xóa đánh giá thành công" })
                            : Results.NotFound(new { message = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa" });
                    }
                    else if (type == "Tour")
                    {
                        var success = await tourReviewService.DeleteTourReviewAsync(reviewId, userId.Value);
                        
                        return success 
                            ? Results.Ok(new { message = "Xóa đánh giá thành công" })
                            : Results.NotFound(new { message = "Không tìm thấy đánh giá hoặc bạn không có quyền xóa" });
                    }
                    else
                    {
                        return Results.BadRequest(new { message = "Query parameter 'type' không hợp lệ. Chỉ chấp nhận 'Hotel' hoặc 'Tour'" });
                    }
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("DeleteFeedback")
            .WithSummary("Xóa đánh giá")
            .WithDescription("Xóa đánh giá của bạn. Query parameter 'type': 'Hotel' hoặc 'Tour'")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization();
        }
    }
}
