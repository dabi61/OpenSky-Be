namespace BE_OPENSKY;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddDatabaseServices(builder.Configuration);
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddApplicationServices();
        
        // Add AutoMapper
        builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
        
        // Add Helpers
        builder.Services.AddScoped<JwtHelper>();
        
        // Add Swagger
        builder.Services.AddSwaggerServices();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        app.UseHttpsRedirection();
        
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

        app.Run();
    }
}
