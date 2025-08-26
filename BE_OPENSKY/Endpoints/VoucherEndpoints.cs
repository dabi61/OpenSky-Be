// using directives đã đưa vào GlobalUsings

namespace BE_OPENSKY.Endpoints;

// Endpoints cho Voucher - API quản lý mã giảm giá
public static class VoucherEndpoints
{
    public static void MapVoucherEndpoints(this WebApplication app)
    {
        var voucherGroup = app.MapGroup("/api/vouchers")
            .WithTags("Vouchers")
            .WithOpenApi();

        // ===== ENDPOINTS CHO ADMIN =====

        // Lấy tất cả voucher (Admin)
        voucherGroup.MapGet("/admin/all", async (IVoucherService voucherService) =>
        {
            var vouchers = await voucherService.GetAllAsync();
            return Results.Ok(vouchers);
        })
        .WithName("AdminGetAllVouchers")
        .WithSummary("Lấy tất cả voucher (Admin)")
        .WithDescription("Admin xem danh sách tất cả voucher trong hệ thống")
        .Produces<IEnumerable<VoucherResponseDTO>>()
        .RequireAuthorization("AdminOnly");

        // Lấy voucher theo ID (Admin)
        voucherGroup.MapGet("/admin/{id:guid}", async (Guid id, IVoucherService voucherService) =>
        {
            var voucher = await voucherService.GetByIdAsync(id);
            return voucher != null ? Results.Ok(voucher) : Results.NotFound();
        })
        .WithName("AdminGetVoucherById")
        .WithSummary("Lấy voucher theo ID (Admin)")
        .WithDescription("Admin xem chi tiết voucher theo ID")
        .Produces<VoucherResponseDTO>()
        .Produces(404)
        .RequireAuthorization("AdminOnly");

        // Lấy voucher theo loại (Admin)
        voucherGroup.MapGet("/admin/type/{tableType}", async (string tableType, IVoucherService voucherService) =>
        {
            if (tableType != "Tour" && tableType != "Hotel")
                return Results.BadRequest(new { message = "Loại voucher phải là 'Tour' hoặc 'Hotel'" });

            var vouchers = await voucherService.GetByTableTypeAsync(tableType);
            return Results.Ok(vouchers);
        })
        .WithName("AdminGetVouchersByType")
        .WithSummary("Lấy voucher theo loại (Admin)")
        .WithDescription("Admin xem voucher theo loại (Tour hoặc Hotel)")
        .Produces<IEnumerable<VoucherResponseDTO>>()
        .Produces(400)
        .RequireAuthorization("AdminOnly");

        // Lấy voucher đang hiệu lực (Admin)
        voucherGroup.MapGet("/admin/active", async (IVoucherService voucherService) =>
        {
            var vouchers = await voucherService.GetActiveVouchersAsync();
            return Results.Ok(vouchers);
        })
        .WithName("AdminGetActiveVouchers")
        .WithSummary("Lấy voucher đang hiệu lực (Admin)")
        .WithDescription("Admin xem danh sách voucher đang có hiệu lực")
        .Produces<IEnumerable<VoucherResponseDTO>>()
        .RequireAuthorization("AdminOnly");

        // Lấy voucher hết hạn (Admin)
        voucherGroup.MapGet("/admin/expired", async (IVoucherService voucherService) =>
        {
            var vouchers = await voucherService.GetExpiredVouchersAsync();
            return Results.Ok(vouchers);
        })
        .WithName("AdminGetExpiredVouchers")
        .WithSummary("Lấy voucher hết hạn (Admin)")
        .WithDescription("Admin xem danh sách voucher đã hết hạn")
        .Produces<IEnumerable<VoucherResponseDTO>>()
        .RequireAuthorization("AdminOnly");

        // Thống kê voucher (Admin)
        voucherGroup.MapGet("/admin/statistics", async (IVoucherService voucherService) =>
        {
            var statistics = await voucherService.GetStatisticsAsync();
            return Results.Ok(statistics);
        })
        .WithName("AdminGetVoucherStatistics")
        .WithSummary("Thống kê voucher (Admin)")
        .WithDescription("Admin xem thống kê tổng quan về voucher")
        .Produces<VoucherStatisticsDTO>()
        .RequireAuthorization("AdminOnly");

        // Tạo voucher mới (Admin)
        voucherGroup.MapPost("/admin", async (VoucherCreateDTO voucherDto, IVoucherService voucherService) =>
        {
            try
            {
                var voucher = await voucherService.CreateAsync(voucherDto);
                return Results.Created($"/api/vouchers/admin/{voucher.VoucherID}", voucher);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AdminCreateVoucher")
        .WithSummary("Tạo voucher mới (Admin)")
        .WithDescription("Admin tạo mã giảm giá mới")
        .Produces<VoucherResponseDTO>(201)
        .Produces(400)
        .RequireAuthorization("AdminOnly");

        // Cập nhật voucher (Admin)
        voucherGroup.MapPut("/admin/{id:guid}", async (Guid id, VoucherUpdateDTO voucherDto, IVoucherService voucherService) =>
        {
            try
            {
                var voucher = await voucherService.UpdateAsync(id, voucherDto);
                return voucher != null ? Results.Ok(voucher) : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AdminUpdateVoucher")
        .WithSummary("Cập nhật voucher (Admin)")
        .WithDescription("Admin chỉnh sửa thông tin voucher")
        .Produces<VoucherResponseDTO>()
        .Produces(400)
        .Produces(404)
        .RequireAuthorization("AdminOnly");

        // Xóa voucher (Admin)
        voucherGroup.MapDelete("/admin/{id:guid}", async (Guid id, IVoucherService voucherService) =>
        {
            try
            {
                var result = await voucherService.DeleteAsync(id);
                return result ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("AdminDeleteVoucher")
        .WithSummary("Xóa voucher (Admin)")
        .WithDescription("Admin xóa voucher (không thể xóa nếu đã có khách hàng sử dụng)")
        .Produces(204)
        .Produces(400)
        .Produces(404)
        .RequireAuthorization("AdminOnly");

        // Xem khách hàng đã lưu voucher (Admin)
        voucherGroup.MapGet("/admin/{id:guid}/users", async (Guid id, IVoucherService voucherService) =>
        {
            var userVouchers = await voucherService.GetVoucherUsersAsync(id);
            return Results.Ok(userVouchers);
        })
        .WithName("AdminGetVoucherUsers")
        .WithSummary("Xem khách hàng đã lưu voucher (Admin)")
        .WithDescription("Admin xem danh sách khách hàng đã lưu voucher này")
        .Produces<IEnumerable<UserVoucherResponseDTO>>()
        .RequireAuthorization("AdminOnly");

        // Đánh dấu voucher đã sử dụng (Admin)
        voucherGroup.MapPost("/admin/mark-used/{userVoucherId:guid}", async (Guid userVoucherId, IVoucherService voucherService) =>
        {
            var result = await voucherService.MarkVoucherAsUsedAsync(userVoucherId);
            return result 
                ? Results.Ok(new { message = "Đã đánh dấu voucher đã sử dụng" })
                : Results.NotFound();
        })
        .WithName("AdminMarkVoucherUsed")
        .WithSummary("Đánh dấu voucher đã sử dụng (Admin)")
        .WithDescription("Admin đánh dấu voucher của khách hàng đã được sử dụng")
        .Produces(200)
        .Produces(404)
        .RequireAuthorization("AdminOnly");

        // ===== ENDPOINTS CHO KHÁCH HÀNG =====

        // Lấy voucher đang hiệu lực (Khách hàng có thể xem)
        voucherGroup.MapGet("/available", async (IVoucherService voucherService) =>
        {
            var vouchers = await voucherService.GetActiveVouchersAsync();
            return Results.Ok(vouchers);
        })
        .WithName("GetAvailableVouchers")
        .WithSummary("Xem voucher có thể lưu")
        .WithDescription("Khách hàng xem danh sách voucher đang có hiệu lực để lưu")
        .Produces<IEnumerable<VoucherResponseDTO>>();

        // Tìm voucher theo mã (Khách hàng)
        voucherGroup.MapGet("/search/{code}", async (string code, IVoucherService voucherService) =>
        {
            var voucher = await voucherService.GetByCodeAsync(code);
            if (voucher == null)
                return Results.NotFound(new { message = "Không tìm thấy voucher với mã này" });

            // Chỉ hiển thị voucher còn hiệu lực
            if (!voucher.IsActive)
                return Results.BadRequest(new { message = "Voucher không còn hiệu lực" });

            return Results.Ok(voucher);
        })
        .WithName("SearchVoucherByCode")
        .WithSummary("Tìm voucher theo mã")
        .WithDescription("Khách hàng tìm kiếm voucher bằng mã code")
        .Produces<VoucherResponseDTO>()
        .Produces(400)
        .Produces(404);

        // Lưu voucher (Khách hàng)
        voucherGroup.MapPost("/save", async (SaveVoucherDTO saveDto, IVoucherService voucherService, HttpContext context) =>
        {
            // Lấy thông tin user từ JWT
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Results.Unauthorized();

            try
            {
                var userVoucher = await voucherService.SaveVoucherAsync(saveDto.Code, userId);
                return userVoucher != null 
                    ? Results.Created("/api/vouchers/my-vouchers", userVoucher)
                    : Results.BadRequest(new { message = "Không thể lưu voucher" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("SaveVoucher")
        .WithSummary("Lưu voucher")
        .WithDescription("Khách hàng lưu voucher vào tài khoản bằng mã code")
        .Produces<UserVoucherResponseDTO>(201)
        .Produces(400)
        .Produces(401)
        .RequireAuthorization("AuthenticatedOnly");

        // Xem voucher đã lưu (Khách hàng)
        voucherGroup.MapGet("/my-vouchers", async (IVoucherService voucherService, HttpContext context) =>
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Results.Unauthorized();

            var userVouchers = await voucherService.GetUserSavedVouchersAsync(userId);
            return Results.Ok(userVouchers);
        })
        .WithName("GetMyVouchers")
        .WithSummary("Xem voucher đã lưu")
        .WithDescription("Khách hàng xem danh sách voucher đã lưu trong tài khoản")
        .Produces<IEnumerable<UserVoucherResponseDTO>>()
        .Produces(401)
        .RequireAuthorization("AuthenticatedOnly");

        // Xóa voucher đã lưu (Khách hàng)
        voucherGroup.MapDelete("/my-vouchers/{userVoucherId:guid}", async (Guid userVoucherId, IVoucherService voucherService, HttpContext context) =>
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Results.Unauthorized();

            // TODO: Kiểm tra userVoucherId có thuộc về userId không (để bảo mật)
            
            var result = await voucherService.RemoveUserVoucherAsync(userVoucherId);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveMyVoucher")
        .WithSummary("Xóa voucher đã lưu")
        .WithDescription("Khách hàng xóa voucher khỏi danh sách đã lưu")
        .Produces(204)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization("AuthenticatedOnly");

        // Xem voucher của user cụ thể (Admin hoặc chính user đó)
        voucherGroup.MapGet("/user/{userId:int}", async (int userId, IVoucherService voucherService, HttpContext context) =>
        {
            // Kiểm tra quyền truy cập
            var currentUserIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            var currentUserRoleClaim = context.User.FindFirst(ClaimTypes.Role);

            if (currentUserIdClaim == null)
                return Results.Unauthorized();

            var currentUserId = int.Parse(currentUserIdClaim.Value);
            var isAdmin = currentUserRoleClaim?.Value == RoleConstants.Admin;

            if (!isAdmin && currentUserId != userId)
                return Results.Forbid();

            var userVouchers = await voucherService.GetUserSavedVouchersAsync(userId);
            return Results.Ok(userVouchers);
        })
        .WithName("GetVouchersByUser")
        .WithSummary("Xem voucher của user")
        .WithDescription("Admin hoặc chính user đó xem voucher đã lưu")
        .Produces<IEnumerable<UserVoucherResponseDTO>>()
        .Produces(401)
        .Produces(403)
        .RequireAuthorization("AuthenticatedOnly");
    }
}