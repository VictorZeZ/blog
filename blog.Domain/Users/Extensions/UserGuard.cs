using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;

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
    }
}
