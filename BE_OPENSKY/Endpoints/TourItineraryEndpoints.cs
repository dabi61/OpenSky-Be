using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints
{
    public static class TourItineraryEndpoints
    {
        public static void MapTourItineraryEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/tour_itinerary")
                .WithTags("TourItinerary")
                .WithOpenApi();

            // POST /tour_itinerary - Tạo tour itinerary mới
            group.MapPost("/", async (
                CreateTourItineraryDTO createTourItineraryDto,
                ITourItineraryService tourItineraryService,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin, Supervisor hoặc TourGuide
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor) && !context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin, Supervisor và TourGuide mới được tạo tour itinerary." }, statusCode: 403);
                    }

                    // Kiểm tra dữ liệu đầu vào
                    if (string.IsNullOrWhiteSpace(createTourItineraryDto.Location))
                    {
                        return Results.Json(new { message = "Địa điểm không được để trống" }, statusCode: 400);
                    }

                    if (createTourItineraryDto.DayNumber <= 0)
                    {
                        return Results.Json(new { message = "Số ngày phải lớn hơn 0" }, statusCode: 400);
                    }

                    var itineraryId = await tourItineraryService.CreateTourItineraryAsync(createTourItineraryDto);

                    return Results.Json(new { 
                        message = "Tạo tour itinerary thành công", 
                        itineraryId = itineraryId 
                    }, statusCode: 201);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi tạo tour itinerary: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("CreateTourItinerary")
            .WithSummary("Tạo tour itinerary mới")
            .WithDescription("Tạo tour itinerary mới. Chỉ Admin, Supervisor và TourGuide mới được tạo tour itinerary.")
            .Produces(201)
            .Produces(400)
            .Produces(403)
            .Produces(500)
            .RequireAuthorization("ManagementRoles");

            // GET /tour_itinerary/{id} - Lấy tour itinerary theo ID
            group.MapGet("/{id:guid}", async (
                Guid id,
                ITourItineraryService tourItineraryService) =>
            {
                try
                {
                    var tourItinerary = await tourItineraryService.GetTourItineraryByIdAsync(id);
                    if (tourItinerary == null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok(tourItinerary);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: $"Có lỗi xảy ra khi lấy tour itinerary: {ex.Message}",
                        statusCode: 500
                    );
                }
            })
            .WithName("GetTourItineraryById")
            .WithSummary("Lấy tour itinerary theo ID")
            .WithDescription("Lấy thông tin chi tiết tour itinerary theo ID")
            .Produces(200)
            .Produces(404)
            .Produces(500);

            // PUT /tour_itinerary/{id} - Cập nhật tour itinerary
            group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateTourItineraryDTO updateTourItineraryDto,
                ITourItineraryService tourItineraryService,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin, Supervisor hoặc TourGuide
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor) && !context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin, Supervisor và TourGuide mới được cập nhật tour itinerary." }, statusCode: 403);
                    }

                    // Kiểm tra dữ liệu đầu vào
                    if (!string.IsNullOrWhiteSpace(updateTourItineraryDto.Location) && updateTourItineraryDto.Location.Length < 1)
                    {
                        return Results.Json(new { message = "Địa điểm không được để trống" }, statusCode: 400);
                    }

                    if (updateTourItineraryDto.DayNumber.HasValue && updateTourItineraryDto.DayNumber.Value <= 0)
                    {
                        return Results.Json(new { message = "Số ngày phải lớn hơn 0" }, statusCode: 400);
                    }

                    var success = await tourItineraryService.UpdateTourItineraryAsync(id, updateTourItineraryDto);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy tour itinerary hoặc không thể cập nhật" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Cập nhật tour itinerary thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi cập nhật tour itinerary: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("UpdateTourItinerary")
            .WithSummary("Cập nhật tour itinerary")
            .WithDescription("Cập nhật thông tin tour itinerary. Chỉ Admin, Supervisor và TourGuide mới được cập nhật tour itinerary.")
            .Produces(200)
            .Produces(400)
            .Produces(403)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("ManagementRoles");

            // GET /tour_itinerary/tour/{tourId} - Lấy tour itinerary theo tour
            group.MapGet("/tour/{tourId:guid}", async (
                Guid tourId,
                ITourItineraryService tourItineraryService) =>
            {
                try
                {
                    var tourItineraries = await tourItineraryService.GetTourItinerariesByTourIdAsync(tourId);

                    return Results.Ok(tourItineraries);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: $"Có lỗi xảy ra khi lấy danh sách tour itinerary theo tour: {ex.Message}",
                        statusCode: 500
                    );
                }
            })
            .WithName("GetTourItinerariesByTourId")
            .WithSummary("Lấy tour itinerary theo tour")
            .WithDescription("Lấy danh sách tour itinerary theo tour ID")
            .Produces(200)
            .Produces(500);

            // PUT /tour_itinerary/delete/{id} - Soft delete tour itinerary
            group.MapPut("/delete/{id:guid}", async (
                Guid id,
                ITourItineraryService tourItineraryService,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin, Supervisor hoặc TourGuide
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor) && !context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin, Supervisor và TourGuide mới được xóa tour itinerary." }, statusCode: 403);
                    }

                    var success = await tourItineraryService.DeleteTourItineraryAsync(id);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy tour itinerary hoặc không thể xóa" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Xóa (soft delete) tour itinerary thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi xóa tour itinerary: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("SoftDeleteTourItinerary")
            .WithSummary("Soft delete tour itinerary")
            .WithDescription("Đặt IsDeleted = true cho tour itinerary. Chỉ Admin, Supervisor và TourGuide mới được xóa (mềm).")
            .Produces(200)
            .Produces(403)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("ManagementRoles");
        }
    }
}
