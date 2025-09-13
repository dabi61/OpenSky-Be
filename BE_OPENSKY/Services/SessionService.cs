namespace BE_OPENSKY.Services
{
    public class SessionService : ISessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public SessionService(ApplicationDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        public async Task<Session> CreateSessionAsync(Guid userId)
        {
            var refreshToken = _jwtHelper.GenerateRefreshToken();
            
            var session = new Session
            {
                SessionID = Guid.NewGuid(),
                UserID = userId,
                RefreshToken = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // Refresh token hết hạn sau 30 ngày
                IsActive = true
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            
            return session;
        }

        public async Task<Session?> GetSessionByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Sessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            var session = await GetSessionByRefreshTokenAsync(refreshToken);
            
            if (session == null || !session.IsActive || session.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        public async Task<Session> RefreshSessionAsync(string refreshToken)
        {
            var session = await GetSessionByRefreshTokenAsync(refreshToken);
            
            if (session == null || !session.IsActive)
            {
                throw new InvalidOperationException("Refresh token không hợp lệ hoặc đã bị vô hiệu hóa");
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                // Tự động vô hiệu hóa session hết hạn
                session.IsActive = false;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Refresh token đã hết hạn");
            }

            // Cập nhật thời gian sử dụng lần cuối
            session.LastUsedAt = DateTime.UtcNow;
            
            // Tùy chọn: Xoay refresh token để bảo mật tốt hơn
            // session.RefreshToken = _jwtHelper.GenerateRefreshToken();
            
            await _context.SaveChangesAsync();
            
            return session;
        }

        public async Task<bool> RevokeSessionAsync(string refreshToken)
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

            if (session == null) return false;

            session.IsActive = false;
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _context.Sessions
                .Where(s => s.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredSessions.Any())
            {
                _context.Sessions.RemoveRange(expiredSessions);
                await _context.SaveChangesAsync();
            }
        }
    }
}
