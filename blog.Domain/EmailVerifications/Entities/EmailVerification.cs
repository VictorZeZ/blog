using blog.Domain.Common;
using blog.Domain.Common.Helpers;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Types;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Types;

namespace blog.Domain.EmailVerifications.Entities
{
    public class EmailVerification : Entity<EmailVerificationId>
    {
        public string CodeHash { get; private set; }
        public EmailVerificationPurpose Purpose { get; private set; }
        public string? TargetEmail { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public EmailVerificationStatus Status { get; private set; }
        public int AttemptCount { get; private set; }

        public UserId UserId { get; private set; }
        public User User { get; private set; } = null!;

        private EmailVerification() : base(EmailVerificationId.Empty) { }

        public EmailVerification(UserId userId, string codeHash, EmailVerificationPurpose purpose, int expiryMinutes, string? targetEmail = null) : base(EmailVerificationId.New())
        {
            if (purpose is EmailVerificationPurpose.ChangeEmail or EmailVerificationPurpose.ConfirmNewEmail && string.IsNullOrWhiteSpace(targetEmail))
                throw new ValidationException("TargetEmail", "TargetEmail is required for ChangeEmail and ConfirmNewEmail purposes");

            UserId = userId;
            CodeHash = codeHash;
            Purpose = purpose;
            TargetEmail = purpose is EmailVerificationPurpose.ChangeEmail or EmailVerificationPurpose.ConfirmNewEmail
                ? EmailNormalizer.Normalize(targetEmail!)
                : null;
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
            Status = EmailVerificationStatus.Active;
        }

        public bool IsValid()
            => Status == EmailVerificationStatus.Active && ExpiresAt > DateTime.UtcNow;

        public bool HasExceededAttempts(int maxAttempts)
            => AttemptCount >= maxAttempts;

        public void RegisterFailedAttempt()
        {
            AttemptCount++;
            MarkAsUpdated();
        }

        public void MarkAsVerified()
        {
            Status = EmailVerificationStatus.Verified;
            MarkAsUpdated();
        }

        public void Revoke()
        {
            Status = EmailVerificationStatus.Revoked;
            MarkAsUpdated();
        }
    }
}
