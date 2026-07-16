using MediatR;

namespace blog.Application.Users.Commands.Register
{
    public class RegisterCommand : IRequest<RegisterResponse>
    {
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }
}
