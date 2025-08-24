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
            // Individual role policies
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleConstants.Admin));
            options.AddPolicy("SupervisorOnly", policy => policy.RequireRole(RoleConstants.Supervisor));
            options.AddPolicy("TourGuideOnly", policy => policy.RequireRole(RoleConstants.TourGuide));
            options.AddPolicy("HotelOnly", policy => policy.RequireRole(RoleConstants.Hotel));
            options.AddPolicy("CustomerOnly", policy => policy.RequireRole(RoleConstants.Customer));
            
            // Combined role policies
            options.AddPolicy("ManagementOnly", policy => policy.RequireRole(RoleConstants.ManagementRoles));
            options.AddPolicy("StaffOnly", policy => policy.RequireRole(RoleConstants.StaffRoles));
            options.AddPolicy("ServiceProviderOnly", policy => policy.RequireRole(RoleConstants.ServiceProviderRoles));
            options.AddPolicy("AuthenticatedOnly", policy => policy.RequireRole(RoleConstants.AuthenticatedRoles));
        });

        return services;
    }
}
