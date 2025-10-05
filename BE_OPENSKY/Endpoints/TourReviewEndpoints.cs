using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BE_OPENSKY.Endpoints
{
    public static class TourReviewEndpoints
    {
        public static void MapTourReviewEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/tour_review")
                .WithTags("TourReview")
                .WithOpenApi();

            // POST /tour_review - Tạo đánh giá tour mới
            group.MapPost("/", async (
                CreateTourReviewDTO createTourReviewDto,
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

                    var reviewId = await tourReviewService.CreateTourReviewAsync(createTourReviewDto, userId.Value);

                    return Results.Json(new { 
                        message = "Tạo đánh giá tour thành công", 
                        reviewId = reviewId 
                    }, statusCode: 201);
                }
                catch (ArgumentException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: 400);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: 400);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi tạo đánh giá tour: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("CreateTourReview")
            .WithSummary("Tạo đánh giá tour mới")
            .WithDescription("Tạo đánh giá cho tour (1-5 sao). Chỉ có thể đánh giá sau khi đã đặt tour và thanh toán thành công.")
            .Produces(201)
            .Produces(400)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // GET /tour_review/{id} - Lấy đánh giá tour theo ID
            group.MapGet("/{id:guid}", async (
                Guid id,
                ITourReviewService tourReviewService) =>
            {
                try
                {
                    var review = await tourReviewService.GetTourReviewByIdAsync(id);
                    if (review == null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok(review);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy đánh giá tour: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetTourReviewById")
            .WithSummary("Lấy đánh giá tour theo ID")
            .WithDescription("Lấy thông tin chi tiết đánh giá tour theo ID")
            .Produces(200)
            .Produces(404)
            .Produces(500);

            // GET /tour_review/tour/{tourId} - Lấy danh sách đánh giá theo tour với phân trang và thống kê
            group.MapGet("/tour/{tourId:guid}", async (
                Guid tourId,
                ITourReviewService tourReviewService,
                int page = 1,
                int limit = 10) =>
            {
                try
                {
                    if (page < 1) page = 1;
                    if (limit < 1 || limit > 100) limit = 10;

                    var result = await tourReviewService.GetPaginatedTourReviewsAsync(tourId, page, limit);

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách đánh giá tour: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetTourReviewsByTourId")
            .WithSummary("Lấy danh sách đánh giá theo tour")
            .WithDescription("Lấy danh sách đánh giá của tour với phân trang và thống kê")
            .Produces(200)
            .Produces(500);

            // GET /tour_review/tour/{tourId}/stats - Lấy thống kê đánh giá tour
            group.MapGet("/tour/{tourId:guid}/stats", async (
                Guid tourId,
                ITourReviewService tourReviewService) =>
            {
                try
                {
                    var stats = await tourReviewService.GetTourReviewStatsAsync(tourId);

                    return Results.Ok(stats);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy thống kê đánh giá tour: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetTourReviewStats")
            .WithSummary("Lấy thống kê đánh giá tour")
            .WithDescription("Lấy thống kê đánh giá tour (điểm trung bình, số lượng đánh giá)")
            .Produces(200)
            .Produces(500);

            // GET /tour_review/tour/{tourId}/eligibility - Kiểm tra điều kiện đánh giá
            group.MapGet("/tour/{tourId:guid}/eligibility", async (
                Guid tourId,
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

                    var eligibility = await tourReviewService.CheckReviewEligibilityAsync(tourId, userId.Value);

                    return Results.Ok(eligibility);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi kiểm tra điều kiện đánh giá: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("CheckTourReviewEligibility")
            .WithSummary("Kiểm tra điều kiện đánh giá tour")
            .WithDescription("Kiểm tra xem user có thể đánh giá tour hay không")
            .Produces(200)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // PUT /tour_review/{id} - Cập nhật đánh giá tour
            group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateTourReviewDTO updateTourReviewDto,
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

                    var success = await tourReviewService.UpdateTourReviewAsync(id, updateTourReviewDto, userId.Value);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy đánh giá hoặc không có quyền cập nhật" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Cập nhật đánh giá tour thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi cập nhật đánh giá tour: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("UpdateTourReview")
            .WithSummary("Cập nhật đánh giá tour")
            .WithDescription("Cập nhật đánh giá tour của chính mình")
            .Produces(200)
            .Produces(401)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization();

            // DELETE /tour_review/{id} - Xóa đánh giá tour
            group.MapDelete("/{id:guid}", async (
                Guid id,
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

                    var success = await tourReviewService.DeleteTourReviewAsync(id, userId.Value);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy đánh giá hoặc không có quyền xóa" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Xóa đánh giá tour thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi xóa đánh giá tour: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("DeleteTourReview")
            .WithSummary("Xóa đánh giá tour")
            .WithDescription("Xóa đánh giá tour của chính mình")
            .Produces(200)
            .Produces(401)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization();

            // GET /tour_review/my-reviews - Lấy đánh giá tour của user hiện tại
            group.MapGet("/my-reviews", async (
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

                    var reviews = await tourReviewService.GetUserTourReviewsAsync(userId.Value);
                    return Results.Ok(reviews);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy đánh giá của bạn: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetMyTourReviews")
            .WithSummary("Xem những đánh giá tour mà tôi đã đánh giá")
            .WithDescription("Lấy danh sách đánh giá tour của user hiện tại")
            .Produces(200)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();
        }
    }
}
