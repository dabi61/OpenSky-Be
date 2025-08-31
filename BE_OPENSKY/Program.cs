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
        
        // Add CORS
        builder.Services.AddCorsServices();
        
        // Add AutoMapper
        builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
        
        // Add Helpers
        builder.Services.AddScoped<JwtHelper>();
        
        // Add Swagger
        builder.Services.AddSwaggerServices();

        var app = builder.Build();

        // Auto-apply migrations on startup (for Railway deployment) - DISABLED
        // using (var scope = app.Services.CreateScope())
        // {
        //     var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //     try
        //     {
        //         dbContext.Database.Migrate();
        //         app.Logger.LogInformation("Database migrations applied successfully");
        //     }
        //     catch (Exception ex)
        //     {
        //         app.Logger.LogError(ex, "Error applying database migrations");
        //         // Không throw exception để app vẫn có thể start
        //     }
        // }

        // Configure the HTTP request pipeline
        app.UseHttpsRedirection();
        
        // Use CORS
        app.UseCors("AllowAll"); // Sử dụng policy cho phép tất cả origins
        
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
        app.MapTourEndpoints();        // Tour management endpoints
        app.MapGoogleAuthEndpoints();  // Google OAuth endpoints
        app.MapVoucherEndpoints();     // Voucher management endpoints
        app.MapImageEndpoints();       // Image management endpoints

        // Redirect root to Swagger
        app.MapGet("/", () => Results.Redirect("/swagger"));

        app.Run();
    }
}
