namespace BE_OPENSKY.DTOs;

public class GoogleAuthRequest
{
    public string IdToken { get; set; } = string.Empty;
}

public class GoogleUserInfo
{
    public string Sub { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Given_Name { get; set; } = string.Empty; // Google uses snake_case
    public string Family_Name { get; set; } = string.Empty; // Google uses snake_case
    public string Picture { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Email_Verified { get; set; } = string.Empty; // Google returns string "true"/"false"
    public string Locale { get; set; } = string.Empty;
    public string Aud { get; set; } = string.Empty; // Audience
}

public class GoogleAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public UserResponseDTO User { get; set; } = new();
    public bool IsNewUser { get; set; }
}
