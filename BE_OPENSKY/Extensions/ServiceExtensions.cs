using Npgsql;

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
        // Ưu tiên lấy từ biến môi trường trước, nếu không có thì dùng từ configuration
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                              ?? configuration.GetConnectionString("DefaultConnection");

        // Chuyển đổi nếu dùng DATABASE_URL từ Railway
        if (connectionString.StartsWith("postgresql://"))
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');

            connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = uri.AbsolutePath.TrimStart('/'),
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            }.ToString();
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
            
        return services;
    }
}
