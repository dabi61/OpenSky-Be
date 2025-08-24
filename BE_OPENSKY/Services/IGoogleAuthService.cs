using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services;

public interface IGoogleAuthService
{
    Task<GoogleAuthResponse> AuthenticateGoogleUserAsync(string idToken);
    Task<GoogleUserInfo> VerifyGoogleTokenAsync(string idToken);
}
