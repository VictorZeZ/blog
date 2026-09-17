using blog.Domain.Users.Enums;

namespace blog.Application.Users.Queries.GetUserDetails
{
    public class GetUserDetailsResponse
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public UserLevel Level { get; init; }
        public bool IsBanned { get; init; }
        public bool IsDeleted { get; init; }
        public bool IsEmailConfirmed { get; init; }
        public bool TwoFactorEnabled { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}