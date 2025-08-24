namespace BE_OPENSKY.Endpoints;

public static class TourEndpoints
{
    public static void MapTourEndpoints(this WebApplication app)
    {
        var tourGroup = app.MapGroup("/api/tours")
            .WithTags("Tours")
            .WithOpenApi();

        // Get all tours
        tourGroup.MapGet("/", async (ITourRepository tourRepository) =>
        {
            var tours = await tourRepository.GetAllAsync();
            return Results.Ok(tours);
        })
        .WithName("GetAllTours")
        .WithSummary("Get all tours")
        .Produces<IEnumerable<Tour>>();

        // Get tour by ID
        tourGroup.MapGet("/{id:int}", async (int id, ITourRepository tourRepository) =>
        {
            var tour = await tourRepository.GetByIdAsync(id);
            return tour != null ? Results.Ok(tour) : Results.NotFound();
        })
        .WithName("GetTourById")
        .WithSummary("Get tour by ID")
        .Produces<Tour>()
        .Produces(404);

        // Get tours by user ID
        tourGroup.MapGet("/user/{userId:int}", async (int userId, ITourRepository tourRepository) =>
        {
            var tours = await tourRepository.GetByUserIdAsync(userId);
            return Results.Ok(tours);
        })
        .WithName("GetToursByUserId")
        .WithSummary("Get tours by user ID")
        .Produces<IEnumerable<Tour>>();

        // Create tour
        tourGroup.MapPost("/", async (TourCreateDTO tourDto, ITourRepository tourRepository, HttpContext httpContext) =>
        {
            // Get user ID from JWT token
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Results.Unauthorized();

            var userId = int.Parse(userIdClaim.Value);
            
            var tour = new Tour
            {
                UserID = userId,
                Address = tourDto.Address,
                NumberOfDays = tourDto.NumberOfDays,
                MaxPeople = tourDto.MaxPeople,
                Description = tourDto.Description,
                Star = tourDto.Star,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            var createdTour = await tourRepository.CreateAsync(tour);
            return Results.Created($"/api/tours/{createdTour.TourID}", createdTour);
        })
        .WithName("CreateTour")
        .WithSummary("Create new tour")
        .Produces<Tour>(201)
        .Produces(401)
        .RequireAuthorization();

        // Update tour
        tourGroup.MapPut("/{id:int}", async (int id, TourUpdateDTO tourDto, ITourRepository tourRepository, HttpContext httpContext) =>
        {
            var existingTour = await tourRepository.GetByIdAsync(id);
            if (existingTour == null)
                return Results.NotFound();

            // Check if user owns this tour or is management
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = httpContext.User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || (int.Parse(userIdClaim.Value) != existingTour.UserID && 
                !RoleConstants.ManagementRoles.Contains(userRoleClaim?.Value)))
                return Results.Forbid();

            // Update only provided fields
            if (!string.IsNullOrEmpty(tourDto.Address))
                existingTour.Address = tourDto.Address;
            if (tourDto.NumberOfDays.HasValue)
                existingTour.NumberOfDays = tourDto.NumberOfDays.Value;
            if (tourDto.MaxPeople.HasValue)
                existingTour.MaxPeople = tourDto.MaxPeople.Value;
            if (!string.IsNullOrEmpty(tourDto.Description))
                existingTour.Description = tourDto.Description;
            if (tourDto.Star.HasValue)
                existingTour.Star = tourDto.Star.Value;
            if (!string.IsNullOrEmpty(tourDto.Status))
                existingTour.Status = tourDto.Status;

            var updatedTour = await tourRepository.UpdateAsync(existingTour);
            return Results.Ok(updatedTour);
        })
        .WithName("UpdateTour")
        .WithSummary("Update tour")
        .Produces<Tour>()
        .Produces(404)
        .Produces(403)
        .RequireAuthorization();

        // Delete tour
        tourGroup.MapDelete("/{id:int}", async (int id, ITourRepository tourRepository, HttpContext httpContext) =>
        {
            var existingTour = await tourRepository.GetByIdAsync(id);
            if (existingTour == null)
                return Results.NotFound();

            // Check if user owns this tour or is admin
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = httpContext.User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || (int.Parse(userIdClaim.Value) != existingTour.UserID && userRoleClaim?.Value != "Admin"))
                return Results.Forbid();

            var result = await tourRepository.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteTour")
        .WithSummary("Delete tour")
        .Produces(204)
        .Produces(404)
        .Produces(403)
        .RequireAuthorization();
    }
}
