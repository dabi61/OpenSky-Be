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
            // ===== CHÍNH SÁCH PHÂN QUYỀN THEO TỪNG VAI TRÒ (ROLE) =====
            // Dùng khi endpoint yêu cầu đúng 1 loại vai trò cụ thể
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleConstants.Admin));            // Chỉ Quản trị (Admin)
            options.AddPolicy("SupervisorOnly", policy => policy.RequireRole(RoleConstants.Supervisor)); // Chỉ Giám sát (Supervisor)
            options.AddPolicy("TourGuideOnly", policy => policy.RequireRole(RoleConstants.TourGuide));   // Chỉ Hướng dẫn viên (Tour guide)
            options.AddPolicy("HotelOnly", policy => policy.RequireRole(RoleConstants.Hotel));           // Chỉ Nhà cung cấp khách sạn (Hotel/Motel)
            options.AddPolicy("CustomerOnly", policy => policy.RequireRole(RoleConstants.Customer));     // Chỉ Khách hàng (Customer)

            // ===== CHÍNH SÁCH NHÓM (GỘP NHIỀU ROLE) =====
            // Mục tiêu: viết gọn .RequireAuthorization("...") ở các endpoint
            // ManagementOnly = Admin + Supervisor (nhóm quản lý)
            //   - Dùng cho các thao tác quản trị/duyệt/xóa ở cấp quản lý
            options.AddPolicy("ManagementOnly", policy => policy.RequireRole(RoleConstants.ManagementRoles));

            // StaffOnly = Admin + Supervisor + TourGuide (nhân sự nội bộ cung cấp tour)
            //   - Dùng cho các chức năng vận hành tour, hỗ trợ khách
            options.AddPolicy("StaffOnly", policy => policy.RequireRole(RoleConstants.StaffRoles));

            // ServiceProviderOnly = Admin + Supervisor + TourGuide + Hotel (bên cung cấp dịch vụ)
            //   - Dùng khi cần cho phép tất cả các bên cung cấp dịch vụ truy cập
            options.AddPolicy("ServiceProviderOnly", policy => policy.RequireRole(RoleConstants.ServiceProviderRoles));

            // AuthenticatedOnly = Tất cả các vai trò hợp lệ (đã đăng nhập)
            //   - Dùng cho các endpoint chỉ cần đăng nhập, không phân biệt vai trò
            options.AddPolicy("AuthenticatedOnly", policy => policy.RequireRole(RoleConstants.AuthenticatedRoles));
        });

        return services;
    }
}
