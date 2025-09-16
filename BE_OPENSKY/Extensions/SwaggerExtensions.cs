using BE_OPENSKY.Models;

namespace BE_OPENSKY.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "BE_OPENSKY Travel Management API",
                Version = "v1",
                Description = "RESTful API cho hệ thống quản lý du lịch",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "BE_OPENSKY Team",
                    Email = "contact@beopensky.com"
                }
            });

            // Add JWT Bearer authentication to Swagger
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "Nhập JWT token vào ô bên dưới (không cần thêm 'Bearer')",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Ensure all endpoints are discovered
            c.CustomSchemaIds(type => type.FullName);
            
            // Display ScheduleStatus enum as string in Swagger
            c.MapType<ScheduleStatus>(() => new Microsoft.OpenApi.Models.OpenApiSchema
            {
                Type = "string",
                Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
                {
                    new Microsoft.OpenApi.Any.OpenApiString(nameof(ScheduleStatus.Active)),
                    new Microsoft.OpenApi.Any.OpenApiString(nameof(ScheduleStatus.End)),
                    new Microsoft.OpenApi.Any.OpenApiString(nameof(ScheduleStatus.Suspend)),
                    new Microsoft.OpenApi.Any.OpenApiString(nameof(ScheduleStatus.Removed))
                }
            });
            
            // Configure file upload support
            c.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            });
            
            // Configure multipart form data for profile update
            c.MapType<UpdateProfileWithAvatarDTO>(() => new Microsoft.OpenApi.Models.OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSchema>
                {
                    ["fullName"] = new Microsoft.OpenApi.Models.OpenApiSchema
                    {
                        Type = "string",
                    },
                    ["phoneNumber"] = new Microsoft.OpenApi.Models.OpenApiSchema
                    {
                        Type = "string",
                    },
                    ["citizenId"] = new Microsoft.OpenApi.Models.OpenApiSchema
                    {
                        Type = "string",
                    },
                    ["doB"] = new Microsoft.OpenApi.Models.OpenApiSchema
                    {
                        Type = "string",
                        Format = "date",
                        Description = "Ngày sinh (dd-MM-yyyy)",
                        Example = new Microsoft.OpenApi.Any.OpenApiString("01-01-1990")
                    },
                    ["avatar"] = new Microsoft.OpenApi.Models.OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary",
                    }
                }
            });
            
            // Include XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    public static WebApplication UseSwaggerServices(this WebApplication app)
    {
        // Enable Swagger in all environments (including production for Railway)
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "BE_OPENSKY API v1");
            c.RoutePrefix = "swagger";
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.ShowExtensions();
        });

        return app;
    }
}
