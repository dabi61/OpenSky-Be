using Microsoft.AspNetCore.Mvc;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Services;
using BE_OPENSKY.Helpers;

namespace BE_OPENSKY.Endpoints;

public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this IEndpointRouteBuilder app)
    {
        var statisticsGroup = app.MapGroup("/statistics")
            .WithTags("Statistics")
            .WithOpenApi();

        // GET /statistics/bills/monthly?year=2024 - Lấy thống kê bill theo tháng (Admin only)
        statisticsGroup.MapGet("/bills/monthly", async (
            [FromServices] IStatisticsService statisticsService,
            int? year) =>
        {
            try
            {
                // Nếu không truyền year thì lấy year hiện tại
                var targetYear = year ?? DateTime.UtcNow.Year;
                
                var result = await statisticsService.GetBillMonthlyStatisticsAsync(targetYear);
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
        .RequireAuthorization(policy => policy.RequireRole(RoleConstants.Admin))
        .WithName("GetBillMonthlyStatistics")
        .WithSummary("Lấy thống kê bill theo tháng (Admin)")
        .WithDescription("Trả về số lượng bill và tổng tiền theo từng tháng trong năm (chỉ tính bill đã thanh toán)")
        .Produces<BillMonthlyStatisticsResponseDTO>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // GET /statistics/users/customers - Lấy số lượng user là Customer (Admin only)
        statisticsGroup.MapGet("/users/customers", async (
            [FromServices] IStatisticsService statisticsService) =>
        {
            try
            {
                var result = await statisticsService.GetCustomerCountAsync();
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
        .RequireAuthorization(policy => policy.RequireRole(RoleConstants.Admin))
        .WithName("GetCustomerCount")
        .WithSummary("Lấy số lượng user là Customer (Admin)")
        .WithDescription("Trả về tổng số user có role là Customer và đang active")
        .Produces<UserCountByRoleDTO>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // GET /statistics/users/supervisors - Lấy số lượng user là Supervisor (Admin only)
        statisticsGroup.MapGet("/users/supervisors", async (
            [FromServices] IStatisticsService statisticsService) =>
        {
            try
            {
                var result = await statisticsService.GetSupervisorCountAsync();
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
        .RequireAuthorization(policy => policy.RequireRole(RoleConstants.Admin))
        .WithName("GetSupervisorCount")
        .WithSummary("Lấy số lượng user là Supervisor (Admin)")
        .WithDescription("Trả về tổng số user có role là Supervisor và đang active")
        .Produces<UserCountByRoleDTO>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // GET /statistics/users/tourguides - Lấy số lượng user là TourGuide (Admin only)
        statisticsGroup.MapGet("/users/tourguides", async (
            [FromServices] IStatisticsService statisticsService) =>
        {
            try
            {
                var result = await statisticsService.GetTourGuideCountAsync();
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
        .RequireAuthorization(policy => policy.RequireRole(RoleConstants.Admin))
        .WithName("GetTourGuideCount")
        .WithSummary("Lấy số lượng user là TourGuide (Admin)")
        .WithDescription("Trả về tổng số user có role là TourGuide và đang active")
        .Produces<UserCountByRoleDTO>(200)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // GET /statistics/hotels?month=1&year=2024 - Lấy số lượng hotel (Admin only)
        statisticsGroup.MapGet("/hotels", async (
            [FromServices] IStatisticsService statisticsService,
            int? month,
            int? year) =>
        {
            try
            {
                // Nếu có month thì phải có year
                if (month.HasValue && !year.HasValue)
                {
                    return Results.BadRequest(new { message = "Nếu truyền tháng thì phải truyền cả năm" });
                }

                // Validate month
                if (month.HasValue && (month.Value < 1 || month.Value > 12))
                {
                    return Results.BadRequest(new { message = "Tháng phải từ 1 đến 12" });
                }

                var result = await statisticsService.GetHotelCountAsync(month, year);
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
        .RequireAuthorization(policy => policy.RequireRole(RoleConstants.Admin))
        .WithName("GetHotelCount")
        .WithSummary("Lấy số lượng hotel (Admin)")
        .WithDescription("Trả về số lượng hotel đang active. Nếu không truyền tháng/năm thì trả về tổng số hotel của hệ thống, nếu có truyền thì trả về số hotel được tạo trong tháng đó")
        .Produces<HotelCountDTO>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // GET /statistics/tours?month=1&year=2024 - Lấy số lượng tour (Admin only)
        statisticsGroup.MapGet("/tours", async (
            [FromServices] IStatisticsService statisticsService,
            int? month,
            int? year) =>
        {
            try
            {
                // Nếu có month thì phải có year
                if (month.HasValue && !year.HasValue)
                {
                    return Results.BadRequest(new { message = "Nếu truyền tháng thì phải truyền cả năm" });
                }

                // Validate month
                if (month.HasValue && (month.Value < 1 || month.Value > 12))
                {
                    return Results.BadRequest(new { message = "Tháng phải từ 1 đến 12" });
                }

                var result = await statisticsService.GetTourCountAsync(month, year);
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
        .RequireAuthorization(policy => policy.RequireRole(RoleConstants.Admin))
        .WithName("GetTourCount")
        .WithSummary("Lấy số lượng tour (Admin)")
        .WithDescription("Trả về số lượng tour đang active. Nếu không truyền tháng/năm thì trả về tổng số tour của hệ thống, nếu có truyền thì trả về số tour được tạo trong tháng đó")
        .Produces<TourCountDTO>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(500);
    }
}

