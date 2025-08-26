namespace BE_OPENSKY.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITourRepository, TourRepository>();
        services.AddScoped<IVoucherRepository, VoucherRepository>();
        
        // Add Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IVoucherService, VoucherService>();
        services.AddScoped<IImageService, ImageService>();
        
        // Add HttpClient for Google OAuth
        services.AddHttpClient();
        
        // Add Cloudinary
        services.AddCloudinaryService(configuration);
        
        return services;
    }

    // Cấu hình Cloudinary
    public static IServiceCollection AddCloudinaryService(this IServiceCollection services, IConfiguration configuration)
    {
        var cloudinarySection = configuration.GetSection("Cloudinary");
        var cloudName = cloudinarySection["CloudName"];
        var apiKey = cloudinarySection["ApiKey"];
        var apiSecret = cloudinarySection["ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            throw new InvalidOperationException("Cloudinary configuration is missing or incomplete");
        }

        var cloudinaryAccount = new CloudinaryDotNet.Account(cloudName, apiKey, apiSecret);
        var cloudinary = new CloudinaryDotNet.Cloudinary(cloudinaryAccount);
        
        services.AddSingleton(cloudinary);
        
        return services;
    }
    
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
            
        return services;
    }
}
