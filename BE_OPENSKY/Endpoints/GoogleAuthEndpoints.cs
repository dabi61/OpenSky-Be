// using directives đã đưa vào GlobalUsings

namespace BE_OPENSKY.Endpoints;

public static class GoogleAuthEndpoints
{
    public static void MapGoogleAuthEndpoints(this WebApplication app)
    {
        var googleAuthGroup = app.MapGroup("/auth/google")
            .WithTags("Google OAuth")
            .WithOpenApi();

        // Google OAuth authentication
        googleAuthGroup.MapPost("/login", async (GoogleAuthRequest request, [FromServices] IGoogleAuthService googleAuthService) =>
        {
            try
            {
                if (string.IsNullOrEmpty(request.IdToken))
                {
                    return Results.BadRequest(new { message = "IdToken is required" });
                }

                var response = await googleAuthService.AuthenticateGoogleUserAsync(request.IdToken);
                return Results.Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Internal Server Error",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("GoogleLogin")
        .WithSummary("Login or register with Google OAuth")
        .WithDescription("Send Google ID Token to authenticate or register user")
        .Produces<GoogleAuthResponse>(200)
        .Produces(400)
        .Produces(500);

        // Test endpoint for development
        googleAuthGroup.MapPost("/test", async (GoogleAuthRequest request, [FromServices] IGoogleAuthService googleAuthService) =>
        {
            try
            {
                if (string.IsNullOrEmpty(request.IdToken))
                {
                    return Results.BadRequest(new { 
                        message = "IdToken is required",
                        example = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjE2MzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDEyMzQ1Njc4OTAiLCJ0eXAiOiJKV1QifQ...",
                        note = "This is a test endpoint. Use a valid Google ID Token for testing."
                    });
                }

                var response = await googleAuthService.AuthenticateGoogleUserAsync(request.IdToken);
                return Results.Ok(new
                {
                    success = true,
                    data = response,
                    message = "Test successful"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { 
                    success = false,
                    message = ex.Message,
                    type = "ValidationError"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Test Failed",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("GoogleTest")
        .WithSummary("Test Google OAuth (Development only)")
        .WithDescription("Test endpoint for Google OAuth authentication. Use this to test your Google ID Token.")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Get Google OAuth configuration
        googleAuthGroup.MapGet("/config", (IConfiguration configuration) =>
        {
            var clientId = configuration["GoogleOAuth:ClientId"];
            var redirectUri = configuration["GoogleOAuth:RedirectUri"];
            
            return Results.Ok(new
            {
                clientId,
                redirectUri,
                authUrl = "https://accounts.google.com/o/oauth2/v2/auth",
                scope = "openid email profile"
            });
        })
        .WithName("GetGoogleConfig")
        .WithSummary("Get Google OAuth configuration")
        .Produces(200);
    }
}
