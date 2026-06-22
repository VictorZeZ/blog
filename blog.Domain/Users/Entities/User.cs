using blog.Domain.Common;
using blog.Domain.Posts.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Types;

namespace blog.Domain.Users.Entities
{
    public class User : Entity<UserId>
    {
        public string Email { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string PasswordHash { get; private set; }
        public UserLevel Level { get; private set; }

        public ICollection<Post> Posts { get; private set; } = [];

        public User(string email, string firstName, string lastName, string passwordHash) : base(UserId.New())
        {
            Email = email.ToLowerInvariant();
            FirstName = firstName;
            LastName = lastName;
            PasswordHash = passwordHash;
            Level = UserLevel.Normal;
        }

        private User() : base(UserId.Empty) { }

        public void ChangePassword(string newPasswordHash)
        {
            PasswordHash = newPasswordHash;
            MarkAsUpdated();
        }

        public void UpdateProfile(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
            MarkAsUpdated();
        }

        public void Promote(UserLevel level)
        {
            Level = level;
            MarkAsUpdated();
        }

        public string FullName => $"{FirstName} {LastName}";
    }
}
