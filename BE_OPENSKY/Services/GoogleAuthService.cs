using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Models;
using BE_OPENSKY.Repositories;
using System.Text.Json;

namespace BE_OPENSKY.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtHelper _jwtHelper;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public GoogleAuthService(
        IUserRepository userRepository,
        JwtHelper jwtHelper,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _userRepository = userRepository;
        _jwtHelper = jwtHelper;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<GoogleAuthResponse> AuthenticateGoogleUserAsync(string idToken)
    {
        try
        {
            // Verify Google ID token
            var googleUserInfo = await VerifyGoogleTokenAsync(idToken);
            
            if (googleUserInfo == null)
            {
                throw new InvalidOperationException("Invalid Google token");
            }

            // Check if user exists
            var existingUser = await _userRepository.GetByEmailAsync(googleUserInfo.Email);
            
            if (existingUser != null)
            {
                // User exists, generate JWT token
                var token = _jwtHelper.GenerateToken(existingUser);
                
                return new GoogleAuthResponse
                {
                    Token = token,
                    Message = "Login successful",
                    User = MapToUserResponseDTO(existingUser),
                    IsNewUser = false
                };
            }
            else
            {
                // Create new user
                var newUser = new User
                {
                    Email = googleUserInfo.Email,
                    FullName = googleUserInfo.Name,
                    Password = string.Empty, // OAuth users don't have passwords
                    Role = RoleConstants.Customer, // Default role for new users
                    ProviderId = googleUserInfo.Sub, // Google user ID
                    AvatarURL = googleUserInfo.Picture,
                    CreatedAt = DateTime.UtcNow
                };

                var createdUser = await _userRepository.CreateAsync(newUser);
                var token = _jwtHelper.GenerateToken(createdUser);

                return new GoogleAuthResponse
                {
                    Token = token,
                    Message = "Registration successful",
                    User = MapToUserResponseDTO(createdUser),
                    IsNewUser = true
                };
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Google authentication failed: {ex.Message}");
        }
    }

    public async Task<GoogleUserInfo> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            // Get Google's public keys to verify the token
            var googleKeysUrl = "https://www.googleapis.com/oauth2/v3/certs";
            var response = await _httpClient.GetAsync(googleKeysUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Failed to fetch Google public keys");
            }

            // For simplicity, we'll use Google's tokeninfo endpoint
            // In production, you should implement proper JWT verification
            var tokenInfoUrl = $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}";
            var tokenResponse = await _httpClient.GetAsync(tokenInfoUrl);
            
            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Invalid Google ID token");
            }

            var tokenInfoJson = await tokenResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            var tokenInfo = JsonSerializer.Deserialize<GoogleUserInfo>(tokenInfoJson, options);

            if (tokenInfo == null)
            {
                throw new InvalidOperationException("Failed to parse Google user info");
            }

            // Verify the token is from Google
            var googleClientId = _configuration["GoogleOAuth:ClientId"];
            if (string.IsNullOrEmpty(googleClientId))
            {
                throw new InvalidOperationException("Google OAuth client ID not configured");
            }

            // Validate audience if available
            if (!string.IsNullOrEmpty(tokenInfo.Aud) && tokenInfo.Aud != googleClientId)
            {
                throw new InvalidOperationException("Invalid token audience");
            }

            return tokenInfo;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Token verification failed: {ex.Message}");
        }
    }

    private UserResponseDTO MapToUserResponseDTO(User user)
    {
        return new UserResponseDTO
        {
            UserID = user.UserID,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            CitizenId = user.CitizenId,
            DoB = user.DoB,
            AvatarURL = user.AvatarURL,
            CreatedAt = user.CreatedAt
        };
    }
}
