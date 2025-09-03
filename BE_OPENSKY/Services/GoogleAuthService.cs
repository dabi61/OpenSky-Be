using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Models;
// Đã loại bỏ repositories layer - sử dụng IUserService thay thế
using System.Text.Json;

namespace BE_OPENSKY.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IUserService _userService;
    private readonly JwtHelper _jwtHelper;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public GoogleAuthService(
        IUserService userService,
        JwtHelper jwtHelper,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _userService = userService;
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

            // Kiểm tra người dùng đã tồn tại
            var existingUser = await _userService.GetByEmailAsync(googleUserInfo.Email);
            
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
                var createdUserDto = await _userService.CreateAsync(new UserRegisterDTO
                {
                    Email = googleUserInfo.Email,
                    FullName = googleUserInfo.Name,
                    Password = Guid.NewGuid().ToString("N"), // placeholder
                    Role = RoleConstants.Customer,
                    ProviderId = googleUserInfo.Sub,
                    AvatarURL = googleUserInfo.Picture
                });
                
                // Lấy user entity để tạo token
                var createdUser = await _userService.GetByEmailAsync(googleUserInfo.Email);
                var token = _jwtHelper.GenerateToken(createdUser!);

                return new GoogleAuthResponse
                {
                    Token = token,
                    Message = "Registration successful",
                    User = MapToUserResponseDTO(createdUser!),
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
