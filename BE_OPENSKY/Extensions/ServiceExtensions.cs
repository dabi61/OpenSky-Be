namespace BE_OPENSKY.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITourRepository, TourRepository>();
        services.AddScoped<IVoucherRepository, VoucherRepository>();
        
        // Add Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddScoped<IVoucherService, VoucherService>();
        
        // Add HttpClient for Google OAuth
        services.AddHttpClient();
        
        return services;
    }
    
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
            
        return services;
    }
}
