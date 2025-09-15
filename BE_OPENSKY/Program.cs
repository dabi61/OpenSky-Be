namespace BE_OPENSKY;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure port for Railway deployment only in production
        var port = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrEmpty(port))
        {
            // Railway deployment - bind to 0.0.0.0 with Railway's PORT
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        }
        // Nếu không có PORT env var, sử dụng launchSettings.json cho local development

        // Add services to the container
        builder.Services.AddDatabaseServices(builder.Configuration);
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddApplicationServices(builder.Configuration);
        
        // Configure form options for file upload
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB limit
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
        });
        
        // Add CORS
        builder.Services.AddCorsServices();
        
        
        // Add Helpers
        builder.Services.AddScoped<JwtHelper>();
        
        // Add Swagger
        builder.Services.AddSwaggerServices();

        var app = builder.Build();

        // Auto-apply migrations on startup (for Railway deployment)
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                dbContext.Database.Migrate();
                app.Logger.LogInformation("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Error applying database migrations");
                // Không throw exception để app vẫn có thể start
            }
        }

        // Configure the HTTP request pipeline
        app.UseHttpsRedirection();
        
        // Use CORS - MUST be before UseAuthentication and UseAuthorization
        app.UseCors("AllowAll"); // Sử dụng policy cho phép tất cả origins
        
        // Handle preflight OPTIONS requests explicitly
        app.Use(async (context, next) =>
        {
            if (context.Request.Method == "OPTIONS")
            {
                var origin = context.Request.Headers["Origin"].FirstOrDefault();
                var allowedOrigins = new[] {
                    "http://localhost:3000", "http://localhost:3001", "http://localhost:4200", 
                    "http://localhost:5173", "http://localhost:8080",
                    "https://localhost:3000", "https://localhost:3001", "https://localhost:4200", 
                    "https://localhost:5173", "https://localhost:8080"
                };
                
                if (allowedOrigins.Contains(origin))
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", origin);
                }
                else
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
                context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                context.Response.Headers.Add("Access-Control-Max-Age", "86400");
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("");
                return;
            }
            await next();
        });
        
        // Serve static files
        app.UseStaticFiles();
        
        // Use Swagger
        app.UseSwaggerServices();
        
        // Use Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map API Endpoints
        app.MapAuthEndpoints();        // Authentication endpoints
        app.MapUserEndpoints();        // User management endpoints
        app.MapGoogleAuthEndpoints();  // Google OAuth endpoints
        app.MapHotelEndpoints();       // Hotel management endpoints
        app.MapHotelRoomEndpoints();   // Hotel room management endpoints
        app.MapHotelReviewEndpoints(); // Hotel review endpoints
        app.MapBookingEndpoints();     // Booking management endpoints
        app.MapBillEndpoints();        // Bill management endpoints
        app.MapTourEndpoints();        // Tour management endpoints
        app.MapScheduleEndpoints();    // Schedule management endpoints
        app.MapScheduleItineraryEndpoints(); // Schedule itinerary management endpoints
        app.MapTourItineraryEndpoints(); // Tour itinerary management endpoints

        // Redirect root to Swagger
        app.MapGet("/", () => Results.Redirect("/swagger"));

        app.Run();
    }
}
