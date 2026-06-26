using blog.Domain.Users.Enums;

namespace blog.Application.Users.Commands.ChangeUserLevel
{
    public class ChangeUserLevelResponse
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public UserLevel Level { get; init; }
    }
}
