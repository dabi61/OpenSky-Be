using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints
{
    public static class ScheduleItineraryEndpoints
    {
        public static void MapScheduleItineraryEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/schedule_itinerary")
                .WithTags("ScheduleItinerary")
                .WithOpenApi();

            // POST /schedule_itinerary - Tạo schedule itinerary mới
            group.MapPost("/", async (
                CreateScheduleItineraryDTO createScheduleItineraryDto,
                IScheduleItineraryService scheduleItineraryService,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin, Supervisor hoặc TourGuide
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor) && !context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin, Supervisor và TourGuide mới được tạo schedule itinerary." }, statusCode: 403);
                    }

                    // Kiểm tra dữ liệu đầu vào
                    if (createScheduleItineraryDto.StartTime >= createScheduleItineraryDto.EndTime)
                    {
                        return Results.Json(new { message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc" }, statusCode: 400);
                    }

                    var scheduleItId = await scheduleItineraryService.CreateScheduleItineraryAsync(createScheduleItineraryDto);

                    return Results.Json(new { 
                        message = "Tạo schedule itinerary thành công", 
                        scheduleItId = scheduleItId 
                    }, statusCode: 201);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi tạo schedule itinerary: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("CreateScheduleItinerary")
            .WithSummary("Tạo schedule itinerary mới")
            .WithDescription("Tạo schedule itinerary mới. Chỉ Admin, Supervisor và TourGuide mới được tạo schedule itinerary.")
            .Produces(201)
            .Produces(400)
            .Produces(403)
            .Produces(500)
            .RequireAuthorization("ManagementRoles");

            // GET /schedule_itinerary/{id} - Lấy schedule itinerary theo ID
            group.MapGet("/{id:guid}", async (
                Guid id,
                IScheduleItineraryService scheduleItineraryService) =>
            {
                try
                {
                    var scheduleItinerary = await scheduleItineraryService.GetScheduleItineraryByIdAsync(id);
                    if (scheduleItinerary == null)
                    {
                        return Results.Json(new { message = "Không tìm thấy schedule itinerary" }, statusCode: 404);
                    }

                    return Results.Json(new
                    {
                        message = "Lấy schedule itinerary thành công",
                        data = scheduleItinerary
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy schedule itinerary: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetScheduleItineraryById")
            .WithSummary("Lấy schedule itinerary theo ID")
            .WithDescription("Lấy thông tin chi tiết schedule itinerary theo ID")
            .Produces(200)
            .Produces(404)
            .Produces(500);

            // PUT /schedule_itinerary/{id} - Cập nhật schedule itinerary
            group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateScheduleItineraryDTO updateScheduleItineraryDto,
                IScheduleItineraryService scheduleItineraryService,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin, Supervisor hoặc TourGuide
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor) && !context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin, Supervisor và TourGuide mới được cập nhật schedule itinerary." }, statusCode: 403);
                    }

                    // Kiểm tra dữ liệu đầu vào
                    if (updateScheduleItineraryDto.StartTime.HasValue && updateScheduleItineraryDto.EndTime.HasValue)
                    {
                        if (updateScheduleItineraryDto.StartTime.Value >= updateScheduleItineraryDto.EndTime.Value)
                        {
                            return Results.Json(new { message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc" }, statusCode: 400);
                        }
                    }

                    var success = await scheduleItineraryService.UpdateScheduleItineraryAsync(id, updateScheduleItineraryDto);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy schedule itinerary hoặc không thể cập nhật" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Cập nhật schedule itinerary thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi cập nhật schedule itinerary: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("UpdateScheduleItinerary")
            .WithSummary("Cập nhật schedule itinerary")
            .WithDescription("Cập nhật thông tin schedule itinerary. Chỉ Admin, Supervisor và TourGuide mới được cập nhật schedule itinerary.")
            .Produces(200)
            .Produces(400)
            .Produces(403)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("ManagementRoles");

            // GET /schedule_itinerary/schedule/{scheduleId} - Lấy schedule itinerary theo schedule
            group.MapGet("/schedule/{scheduleId:guid}", async (
                Guid scheduleId,
                IScheduleItineraryService scheduleItineraryService) =>
            {
                try
                {
                    var scheduleItineraries = await scheduleItineraryService.GetScheduleItinerariesByScheduleIdAsync(scheduleId);

                    return Results.Json(new
                    {
                        message = "Lấy danh sách schedule itinerary theo schedule thành công",
                        data = scheduleItineraries
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách schedule itinerary theo schedule: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetScheduleItinerariesByScheduleId")
            .WithSummary("Lấy schedule itinerary theo schedule")
            .WithDescription("Lấy danh sách schedule itinerary theo schedule ID")
            .Produces(200)
            .Produces(500);

            // DELETE /schedule_itinerary/{id} - Xóa schedule itinerary
            group.MapDelete("/{id:guid}", async (
                Guid id,
                IScheduleItineraryService scheduleItineraryService,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin, Supervisor hoặc TourGuide
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor) && !context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin, Supervisor và TourGuide mới được xóa schedule itinerary." }, statusCode: 403);
                    }

                    var success = await scheduleItineraryService.DeleteScheduleItineraryAsync(id);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy schedule itinerary hoặc không thể xóa" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Xóa schedule itinerary thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi xóa schedule itinerary: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("DeleteScheduleItinerary")
            .WithSummary("Xóa schedule itinerary")
            .WithDescription("Xóa schedule itinerary. Chỉ Admin, Supervisor và TourGuide mới được xóa schedule itinerary.")
            .Produces(200)
            .Produces(403)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("ManagementRoles");
        }
    }
}
