using blog.Domain.Users.Enums;

namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class DashboardProfileResponse
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public UserLevel Level { get; init; }
        public bool IsEmailConfirmed { get; init; }
        public bool TwoFactorEnabled { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
