using blog.Domain.Users.Entities;

namespace blog.Domain.Common.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
    }
}
