using blog.Domain.Common;
using blog.Domain.Tokens.Enums;
using blog.Domain.Tokens.Types;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Types;

namespace blog.Domain.Tokens.Entities
{
    public class RefreshToken : Entity<RefreshTokenId>
    {
        public string Token { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public TokenStatus Status { get; private set; }
        public string DeviceInfo { get; private set; }

        public UserId UserId { get; private set; }
        public User User { get; private set; } = null!;

        private RefreshToken() : base(RefreshTokenId.Empty) { }

        public RefreshToken(string token, UserId userId, string deviceInfo, int expiryDays = 30) : base(RefreshTokenId.New())
        {
            Token = token;
            UserId = userId;
            DeviceInfo = deviceInfo;
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays);
            Status = TokenStatus.Active;
        }

        public bool IsValid()
            => Status == TokenStatus.Active && ExpiresAt > DateTime.UtcNow;

        public RefreshToken Rotate(string newToken, string deviceInfo, int expiryDays = 30)
        {
            Status = TokenStatus.Used;
            MarkAsUpdated();

            return new RefreshToken(newToken, UserId, deviceInfo, expiryDays);
        }

        public void Revoke()
        {
            Status = TokenStatus.Revoked;
            MarkAsUpdated();
        }
    }
}
