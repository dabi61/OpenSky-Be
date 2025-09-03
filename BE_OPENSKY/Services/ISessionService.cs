namespace BE_OPENSKY.Services
{
    public interface ISessionService
    {
        Task<Session> CreateSessionAsync(Guid userId);
        Task<Session?> GetSessionByRefreshTokenAsync(string refreshToken);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);
        Task<Session> RefreshSessionAsync(string refreshToken);
        Task<bool> RevokeSessionAsync(string refreshToken);
        Task CleanupExpiredSessionsAsync();
    }
}
