namespace BE_OPENSKY.Extensions;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            // ===== CHÍNH SÁCH PHÂN QUYỀN THEO TỪNG VAI TRÒ =====
            // Sử dụng khi endpoint yêu cầu đúng một loại vai trò cụ thể
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleConstants.Admin));            // Chỉ Quản trị viên
            options.AddPolicy("SupervisorOnly", policy => policy.RequireRole(RoleConstants.Supervisor)); // Chỉ Giám sát viên
            options.AddPolicy("TourGuideOnly", policy => policy.RequireRole(RoleConstants.TourGuide));   // Chỉ Hướng dẫn viên du lịch
            options.AddPolicy("HotelOnly", policy => policy.RequireRole(RoleConstants.Hotel));           // Chỉ Nhà cung cấp khách sạn
            options.AddPolicy("CustomerOnly", policy => policy.RequireRole(RoleConstants.Customer));     // Chỉ Khách hàng

            // Chính sách cho người dùng đã xác thực (bất kỳ vai trò nào)
            options.AddPolicy("AuthenticatedOnly", policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
