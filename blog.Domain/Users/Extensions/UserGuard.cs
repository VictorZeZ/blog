using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Types;

namespace blog.Domain.Users.Extensions
{
    public static class UserGuard
    {
        public static void EnsureActive(this User user)
        {
            if (user.IsDeleted)
                throw new InvalidStateException("User", "Deleted", "Active");

            if (user.IsBanned)
                throw new InvalidStateException("User", "Banned", "Active");
        }

        public static void EnsureNotLockedOut(this User user)
        {
            if (user.IsLockedOut())
                throw new LockedException("User", user.LockedOutUntil!.Value);
        }

        public static void EnsureEmailConfirmed(this User user)
        {
            if (!user.IsEmailConfirmed)
                throw new EmailNotConfirmedException(user.Email);
        }

        public static bool IsOwner(this User user)
            => user.Level == UserLevel.Owner;

        public static bool IsElevated(this User user)
            => user.Level is UserLevel.Admin or UserLevel.Owner;

        public static bool IsAuthorOrHigher(this User user)
            => user.Level >= UserLevel.Author;

        public static bool CanManagePost(this User user, UserId postAuthorId)
            => user.Id == postAuthorId || user.IsElevated();
    }
}
