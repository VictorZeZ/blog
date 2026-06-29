using blog.Domain.Users.Enums;

namespace blog.Api.DTOs.Users
{
    public class ChangeUserLevelRequest
    {
        public UserLevel Level { get; init; }
    }
}
