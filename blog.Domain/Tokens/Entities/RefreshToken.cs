using blog.Domain.Common;
using blog.Domain.Tokens.Enums;
using blog.Domain.Tokens.Types;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Types;

namespace blog.Domain.Tokens.Entities
{
    public class RefreshToken : Entity<RefreshTokenId>
    {
        public string TokenHash { get; private set; } = null!;
        public DateTime ExpiresAt { get; private set; }
        public TokenStatus Status { get; private set; }
        public string DeviceInfo { get; private set; } = null!;

        public SessionId SessionId { get; private set; }

        public UserId UserId { get; private set; }
        public User User { get; private set; } = null!;

        private RefreshToken() : base(RefreshTokenId.Empty) { }

        public RefreshToken(string tokenHash, UserId userId, string deviceInfo, int expiryDays)
            : this(tokenHash, userId, SessionId.New(), deviceInfo, expiryDays)
        { }

        private RefreshToken(string tokenHash, UserId userId, SessionId sessionId, string deviceInfo, int expiryDays) : base(RefreshTokenId.New())
        {
            TokenHash = tokenHash;
            UserId = userId;
            SessionId = sessionId;
            DeviceInfo = deviceInfo;
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays);
            Status = TokenStatus.Active;
        }

        public bool IsValid()
            => Status == TokenStatus.Active && ExpiresAt > DateTime.UtcNow;

        public RefreshToken Rotate(string newTokenHash, string deviceInfo, int expiryDays)
        {
            Status = TokenStatus.Used;
            MarkAsUpdated();

            return new RefreshToken(newTokenHash, UserId, SessionId, deviceInfo, expiryDays);
        }

        public void Revoke()
        {
            Status = TokenStatus.Revoked;
            MarkAsUpdated();
        }
    }
}