using MediatR;

namespace blog.Application.Users.Commands.ResendRegistrationCode
{
    public class ResendRegistrationCodeCommand : IRequest<ResendRegistrationCodeResponse>
    {
        public string Email { get; init; } = string.Empty;
    }
}
