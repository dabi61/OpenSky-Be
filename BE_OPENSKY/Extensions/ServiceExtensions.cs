using Npgsql;
using StackExchange.Redis;

namespace BE_OPENSKY.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Thêm Redis
        var redisConnectionString = configuration.GetConnectionString("Redis") 
            ?? configuration.GetValue<string>("Redis:ConnectionString")
            ?? "localhost:6379";
        
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
            return ConnectionMultiplexer.Connect(redisConfig);
        });
        
        services.AddScoped<IRedisService, RedisService>();
        
        // Thêm các Services
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IHotelService, HotelService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IEmailService, EmailService>();
        
        // Thêm HttpClient cho Google OAuth
        services.AddHttpClient();
        
        return services;
    }


    
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Ưu tiên lấy từ biến môi trường trước, nếu không có thì dùng từ cấu hình
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                              ?? configuration.GetConnectionString("DefaultConnection");

        // Chuyển đổi nếu sử dụng DATABASE_URL từ Railway
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
                SslMode = SslMode.Require
            }.ToString();
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
            
        return services;
    }

    // Cấu hình CORS
    public static IServiceCollection AddCorsServices(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            // Chính sách riêng cho môi trường sản xuất - có thể tùy chỉnh sau
            options.AddPolicy("Production", policy =>
            {
                policy.WithOrigins(
                    "https://your-frontend-domain.com", // Thay bằng domain frontend thực tế
                    "http://localhost:3000",             // React dev server
                    "http://localhost:3001",             // Cổng thay thế
                    "http://localhost:4200",             // Angular dev server
                    "http://localhost:5173",             // Vite dev server
                    "http://localhost:8080"              // Vue dev server
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials(); // Cho phép cookies/thông tin xác thực
            });
        });

        return services;
    }
}
