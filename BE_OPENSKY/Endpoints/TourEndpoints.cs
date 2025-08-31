namespace BE_OPENSKY.Endpoints;

public static class TourEndpoints
{
    public static void MapTourEndpoints(this WebApplication app)
    {
        var tourGroup = app.MapGroup("/api/tours")
            .WithTags("Tours")
            .WithOpenApi();

        // Lấy tất cả tour
        tourGroup.MapGet("/", async (ITourRepository tourRepository) =>
        {
            var tours = await tourRepository.GetAllAsync();
            return Results.Ok(tours);
        })
        .WithName("GetAllTours")
        .WithSummary("Lấy tất cả tour")
        .WithDescription("Lấy danh sách tất cả tour trong hệ thống")
        .Produces<IEnumerable<Tour>>();

        // Lấy tour theo ID
        tourGroup.MapGet("/{id:guid}", async (Guid id, ITourRepository tourRepository) =>
        {
            var tour = await tourRepository.GetByIdAsync(id);
            return tour != null ? Results.Ok(tour) : Results.NotFound();
        })
        .WithName("GetTourById")
        .WithSummary("Lấy tour theo ID")
        .WithDescription("Lấy thông tin chi tiết tour theo ID")
        .Produces<Tour>()
        .Produces(404);

        // Lấy tour theo user ID
        tourGroup.MapGet("/user/{userId:guid}", async (Guid userId, ITourRepository tourRepository) =>
        {
            var tours = await tourRepository.GetByUserIdAsync(userId);
            return Results.Ok(tours);
        })
        .WithName("GetToursByUserId")
        .WithSummary("Lấy tour theo user ID")
        .WithDescription("Lấy danh sách tour của một user cụ thể")
        .Produces<IEnumerable<Tour>>();

        // Tạo tour mới
        tourGroup.MapPost("/", async (TourCreateDTO tourDto, ITourRepository tourRepository, HttpContext httpContext) =>
        {
            // Get user ID from JWT token
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Results.Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            
            var tour = new Tour
            {
                UserID = userId,
                Name = tourDto.Name,
                Address = tourDto.Address,
                NumberOfDays = tourDto.NumberOfDays,
                MaxPeople = tourDto.MaxPeople,
                Price = tourDto.Price,
                Description = tourDto.Description,
                Star = tourDto.Star,
                Status = TourStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var createdTour = await tourRepository.CreateAsync(tour);
            return Results.Created($"/api/tours/{createdTour.TourID}", createdTour);
        })
        .WithName("CreateTour")
        .WithSummary("Tạo tour mới")
        .WithDescription("Tạo tour mới (yêu cầu đăng nhập)")
        .Produces<Tour>(201)
        .Produces(401)
        .RequireAuthorization();

        // Cập nhật tour
        tourGroup.MapPut("/{id:guid}", async (Guid id, TourUpdateDTO tourDto, ITourRepository tourRepository, HttpContext httpContext) =>
        {
            var existingTour = await tourRepository.GetByIdAsync(id);
            if (existingTour == null)
                return Results.NotFound();

            // Check if user owns this tour or is management
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = httpContext.User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || (Guid.Parse(userIdClaim.Value) != existingTour.UserID && 
                !RoleConstants.ManagementRoles.Contains(userRoleClaim?.Value)))
                return Results.Forbid();

            // Update only provided fields
            if (!string.IsNullOrEmpty(tourDto.Name))
                existingTour.Name = tourDto.Name;
            if (!string.IsNullOrEmpty(tourDto.Address))
                existingTour.Address = tourDto.Address;
            if (tourDto.NumberOfDays.HasValue)
                existingTour.NumberOfDays = tourDto.NumberOfDays.Value;
            if (tourDto.MaxPeople.HasValue)
                existingTour.MaxPeople = tourDto.MaxPeople.Value;
            if (tourDto.Price.HasValue)
                existingTour.Price = tourDto.Price.Value;
            if (!string.IsNullOrEmpty(tourDto.Description))
                existingTour.Description = tourDto.Description;
            if (tourDto.Star.HasValue)
                existingTour.Star = tourDto.Star.Value;
            if (tourDto.Status.HasValue)
                existingTour.Status = tourDto.Status.Value;

            var updatedTour = await tourRepository.UpdateAsync(existingTour);
            return Results.Ok(updatedTour);
        })
        .WithName("UpdateTour")
        .WithSummary("Cập nhật tour")
        .WithDescription("Cập nhật thông tin tour (chỉ chủ sở hữu hoặc Management)")
        .Produces<Tour>()
        .Produces(404)
        .Produces(403)
        .RequireAuthorization();

        // Xóa tour
        tourGroup.MapDelete("/{id:guid}", async (Guid id, ITourRepository tourRepository, HttpContext httpContext) =>
        {
            var existingTour = await tourRepository.GetByIdAsync(id);
            if (existingTour == null)
                return Results.NotFound();

            // Check if user owns this tour or is admin
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = httpContext.User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || (Guid.Parse(userIdClaim.Value) != existingTour.UserID && userRoleClaim?.Value != "Admin"))
                return Results.Forbid();

            var result = await tourRepository.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteTour")
        .WithSummary("Xóa tour")
        .WithDescription("Xóa tour (chỉ chủ sở hữu hoặc Admin)")
        .Produces(204)
        .Produces(404)
        .Produces(403)
        .RequireAuthorization();
    }
}
