using BE_OPENSKY.Data;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Endpoints
{
    public static class ScheduleEndpoints
    {
        public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/schedules")
                .WithTags("Schedule")
                .WithOpenApi();

            // POST /schedules - Tạo schedule mới
            group.MapPost("/", async (
                CreateScheduleDTO createScheduleDto,
                IScheduleService scheduleService,
                ApplicationDbContext dbContext,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin hoặc Supervisor (chỉ Supervisor mới được tạo schedule)
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được tạo schedule." }, statusCode: 403);
                    }

                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Không thể xác định người dùng" }, statusCode: 401);
                    }

                    // Kiểm tra dữ liệu đầu vào
                    if (createScheduleDto.StartTime >= createScheduleDto.EndTime)
                    {
                        return Results.Json(new { message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc" }, statusCode: 400);
                    }

                    if (createScheduleDto.NumberPeople <= 0)
                    {
                        return Results.Json(new { message = "Số lượng người phải lớn hơn 0" }, statusCode: 400);
                    }

                    // Kiểm tra TourGuideID có tồn tại và có role TourGuide không
                    var tourGuide = await dbContext.Users
                        .FirstOrDefaultAsync(u => u.UserID == createScheduleDto.TourGuideID && u.Role == RoleConstants.TourGuide);
                    
                    if (tourGuide == null)
                    {
                        return Results.Json(new { message = "TourGuide không tồn tại hoặc không có quyền TourGuide" }, statusCode: 400);
                    }

                    var scheduleId = await scheduleService.CreateScheduleAsync(userId, createScheduleDto);

                    return Results.Json(new { 
                        message = "Tạo schedule thành công", 
                        scheduleId = scheduleId 
                    }, statusCode: 201);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi tạo schedule: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("CreateSchedule")
            .WithSummary("Tạo schedule mới")
            .WithDescription("Tạo schedule mới và phân công cho TourGuide. Chỉ Admin và Supervisor mới được tạo schedule. Cần cung cấp TourGuideID để phân công.")
            .Produces(201)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(500)
            .RequireAuthorization("SupervisorOrAdmin");

            // GET /schedules?page=1&size=10 - Lấy danh sách schedule có phân trang
            group.MapGet("/", async (
                IScheduleService scheduleService,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var result = await scheduleService.GetSchedulesAsync(page, size);

                    return Results.Json(new
                    {
                        message = "Lấy danh sách schedule thành công",
                        data = result
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách schedule: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetSchedules")
            .WithSummary("Lấy danh sách schedule")
            .WithDescription("Lấy danh sách schedule có phân trang")
            .Produces(200)
            .Produces(500);

            // GET /schedules/{id} - Lấy schedule theo ID
            group.MapGet("/{id:guid}", async (
                Guid id,
                IScheduleService scheduleService) =>
            {
                try
                {
                    var schedule = await scheduleService.GetScheduleByIdAsync(id);
                    if (schedule == null)
                    {
                        return Results.Json(new { message = "Không tìm thấy schedule" }, statusCode: 404);
                    }

                    return Results.Json(new
                    {
                        message = "Lấy schedule thành công",
                        data = schedule
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy schedule: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetScheduleById")
            .WithSummary("Lấy schedule theo ID")
            .WithDescription("Lấy thông tin chi tiết schedule theo ID")
            .Produces(200)
            .Produces(404)
            .Produces(500);

            // PUT /schedules/{id} - Cập nhật schedule (chỉ thời gian và status)
            group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateScheduleDTO updateScheduleDto,
                IScheduleService scheduleService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Không thể xác định người dùng" }, statusCode: 401);
                    }

                    // Kiểm tra quyền Admin, Supervisor hoặc TourGuide
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor) && !context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin, Supervisor và TourGuide mới được cập nhật schedule." }, statusCode: 403);
                    }

                    // Nếu là TourGuide, kiểm tra schedule có được phân công cho họ không
                    if (context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        var isAssigned = await scheduleService.IsScheduleAssignedToTourGuideAsync(id, userId);
                        if (!isAssigned)
                        {
                            return Results.Json(new { message = "Bạn chỉ có thể cập nhật schedule được phân công cho bạn" }, statusCode: 403);
                        }
                    }

                    // Kiểm tra dữ liệu đầu vào
                    if (updateScheduleDto.StartTime.HasValue && updateScheduleDto.EndTime.HasValue)
                    {
                        if (updateScheduleDto.StartTime.Value >= updateScheduleDto.EndTime.Value)
                        {
                            return Results.Json(new { message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc" }, statusCode: 400);
                        }
                    }

                    var success = await scheduleService.UpdateScheduleAsync(id, updateScheduleDto);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy schedule hoặc không thể cập nhật" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Cập nhật schedule thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi cập nhật schedule: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("UpdateSchedule")
            .WithSummary("Cập nhật schedule")
            .WithDescription("Cập nhật thời gian và status của schedule. Chỉ Admin, Supervisor và TourGuide mới được cập nhật schedule.")
            .Produces(200)
            .Produces(400)
            .Produces(403)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("ManagementRoles");

            // GET /schedules/tour/{tourId}?page=1&size=10 - Tìm kiếm schedule theo tour
            group.MapGet("/tour/{tourId:guid}", async (
                Guid tourId,
                IScheduleService scheduleService,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var result = await scheduleService.GetSchedulesByTourIdAsync(tourId, page, size);

                    return Results.Json(new
                    {
                        message = "Lấy danh sách schedule theo tour thành công",
                        data = result
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách schedule theo tour: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetSchedulesByTourId")
            .WithSummary("Lấy schedule theo tour")
            .WithDescription("Lấy danh sách schedule theo tour ID có phân trang")
            .Produces(200)
            .Produces(500);

            // GET /schedules/my-assignments?page=1&size=10 - TourGuide xem schedule được phân công
            group.MapGet("/my-assignments", async (
                IScheduleService scheduleService,
                HttpContext context,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Không thể xác định người dùng" }, statusCode: 401);
                    }

                    // Kiểm tra quyền TourGuide
                    if (!context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ TourGuide mới được xem schedule được phân công." }, statusCode: 403);
                    }

                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var result = await scheduleService.GetSchedulesByTourGuideIdAsync(userId, page, size);

                    return Results.Json(new
                    {
                        message = "Lấy danh sách schedule được phân công thành công",
                        data = result
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách schedule được phân công: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetMyScheduleAssignments")
            .WithSummary("Lấy schedule được phân công cho TourGuide")
            .WithDescription("TourGuide xem danh sách schedule được phân công cho họ")
            .Produces(200)
            .Produces(401)
            .Produces(403)
            .Produces(500)
            .RequireAuthorization("TourGuideOnly");

            // PUT /schedules/delete/{id} - Soft delete schedule
            group.MapPut("/delete/{id:guid}", async (
                Guid id,
                IScheduleService scheduleService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Không thể xác định người dùng" }, statusCode: 401);
                    }

                    // Kiểm tra quyền Admin, Supervisor hoặc TourGuide
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor) && !context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin, Supervisor và TourGuide mới được xóa schedule." }, statusCode: 403);
                    }

                    // Nếu là TourGuide, kiểm tra schedule có được phân công cho họ không
                    if (context.User.IsInRole(RoleConstants.TourGuide))
                    {
                        var isAssigned = await scheduleService.IsScheduleAssignedToTourGuideAsync(id, userId);
                        if (!isAssigned)
                        {
                            return Results.Json(new { message = "Bạn chỉ có thể xóa schedule được phân công cho bạn" }, statusCode: 403);
                        }
                    }

                    var success = await scheduleService.SoftDeleteScheduleAsync(id);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy schedule hoặc không thể xóa" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Xóa schedule thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi xóa schedule: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("SoftDeleteSchedule")
            .WithSummary("Soft delete schedule")
            .WithDescription("Xóa schedule (chuyển trạng thái thành Removed). Chỉ Admin, Supervisor và TourGuide mới được xóa schedule.")
            .Produces(200)
            .Produces(403)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("ManagementRoles");
        }
    }
}
