namespace BE_OPENSKY.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Thêm Redis - Railway compatibility
        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_URL") // Railway Redis
            ?? configuration.GetConnectionString("Redis") 
            ?? configuration.GetValue<string>("Redis:ConnectionString")
            ?? "localhost:6379"; // Local development fallback
        
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
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IHotelReviewService, HotelReviewService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBillService, BillService>();
        services.AddScoped<IQRPaymentService, QRPaymentService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<ITourService, TourService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IScheduleItineraryService, ScheduleItineraryService>();
        services.AddScoped<ITourItineraryService, TourItineraryService>();
        services.AddScoped<ITourReviewService, TourReviewService>();
        services.AddScoped<IVoucherService, VoucherService>();
        services.AddScoped<IUserVoucherService, UserVoucherService>();
        services.AddScoped<IRefundService, RefundService>();
        services.AddScoped<ITourBookingService, TourBookingService>();
        
        // Email Service - SMTP (SendGrid SMTP for Railway, Gmail SMTP for local)
        try
        {
            services.AddScoped<IEmailService, EmailService>();
            Console.WriteLine("[DEBUG] SMTP email service registered successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to register SMTP service: {ex.Message}");
            throw;
        }
        
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
                policy.WithOrigins(
                    "http://localhost:3000",             // React dev server
                    "http://localhost:3001",             // Cổng thay thế
                    "http://localhost:4200",             // Angular dev server
                    "http://localhost:5173",             // Vite dev server
                    "http://localhost:8080",             // Vue dev server
                    "https://localhost:3000",            // HTTPS versions
                    "https://localhost:3001",
                    "https://localhost:4200",
                    "https://localhost:5173",
                    "https://localhost:8080",
                    "https://opesky.vercel.app"          // Frontend production URL
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials() // Cho phép gửi cookies/credentials
                .SetPreflightMaxAge(TimeSpan.FromSeconds(86400)); // Cache preflight for 24 hours
            });

            // Chính sách riêng cho môi trường sản xuất - có thể tùy chỉnh sau
            options.AddPolicy("Production", policy =>
            {
                policy.WithOrigins(
                    "https://opesky.vercel.app",         // Frontend production URL
                    "http://localhost:3000",             // React dev server
                    "http://localhost:3001",             // Cổng thay thế
                    "http://localhost:4200",             // Angular dev server
                    "http://localhost:5173",             // Vite dev server
                    "http://localhost:8080"              // Vue dev server
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials() // Cho phép cookies/thông tin xác thực
                .SetPreflightMaxAge(TimeSpan.FromSeconds(86400)); // Cache preflight for 24 hours
            });
        });

        return services;
    }
}
