using blog.Domain.Common;
using blog.Domain.Common.Helpers;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Entities;
using blog.Domain.Tokens.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Types;

namespace blog.Domain.Users.Entities
{
    public class User : SoftDeletableEntity<UserId>
    {
        public string Email { get; private set; } = null!;
        public string FirstName { get; private set; } = null!;
        public string LastName { get; private set; } = null!;
        public string PasswordHash { get; private set; } = null!;
        public UserLevel Level { get; private set; }

        public bool IsBanned { get; private set; }
        public DateTime? BannedAt { get; private set; }

        public int FailedLoginAttempts { get; private set; }
        public DateTime? LastFailedLoginAt { get; private set; }
        public DateTime? LockedOutUntil { get; private set; }

        public bool IsEmailConfirmed { get; private set; }
        public DateTime? EmailConfirmedAt { get; private set; }

        public bool TwoFactorEnabled { get; private set; }

        public ICollection<Post> Posts { get; private set; } = [];
        public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

        public User(string email, string firstName, string lastName, string passwordHash) : base(UserId.New())
        {
            Email = EmailNormalizer.Normalize(email);
            FirstName = firstName;
            LastName = lastName;
            PasswordHash = passwordHash;
            Level = UserLevel.Normal;
        }

        private User() : base(UserId.Empty) { }

        public void ChangePassword(string newPasswordHash)
        {
            PasswordHash = newPasswordHash;
            ResetFailedLoginAttempts();
            MarkAsUpdated();
        }

        public void UpdateProfile(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
            MarkAsUpdated();
        }

        public void Ban()
        {
            IsBanned = true;
            BannedAt = DateTime.UtcNow;
            MarkAsUpdated();
        }

        public void Unban()
        {
            IsBanned = false;
            BannedAt = null;
            MarkAsUpdated();
        }

        public void Promote(UserLevel level)
        {
            Level = level;
            MarkAsUpdated();
        }

        public bool IsLockedOut()
            => LockedOutUntil is not null && LockedOutUntil > DateTime.UtcNow;

        public void ResetFailedLoginAttempts()
        {
            FailedLoginAttempts = 0;
            LastFailedLoginAt = null;
            LockedOutUntil = null;
            MarkAsUpdated();
        }

        public void ConfirmEmail()
        {
            IsEmailConfirmed = true;
            EmailConfirmedAt = DateTime.UtcNow;
            MarkAsUpdated();
        }

        public bool IsAwaitingConfirmation()
            => !IsEmailConfirmed && !IsDeleted && !IsBanned;

        public void RestartRegistration(string firstName, string lastName, string passwordHash)
        {
            if (!IsAwaitingConfirmation())
                throw new InvalidStateException("User", IsEmailConfirmed ? "Confirmed" : "Deleted or banned", "Unconfirmed");

            FirstName = firstName;
            LastName = lastName;
            PasswordHash = passwordHash;
            ResetFailedLoginAttempts();
        }

        public void ChangeEmail(string newEmail)
        {
            Email = EmailNormalizer.Normalize(newEmail);
            IsEmailConfirmed = true;
            EmailConfirmedAt = DateTime.UtcNow;
            MarkAsUpdated();
        }

        public void EnableTwoFactor()
        {
            TwoFactorEnabled = true;
            MarkAsUpdated();
        }

        public void DisableTwoFactor()
        {
            TwoFactorEnabled = false;
            MarkAsUpdated();
        }

        public string FullName => $"{FirstName} {LastName}";
    }
}
