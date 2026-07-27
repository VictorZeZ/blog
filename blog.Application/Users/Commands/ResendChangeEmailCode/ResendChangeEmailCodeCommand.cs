using MediatR;

namespace blog.Application.Users.Commands.ResendChangeEmailCode
{
    public class ResendChangeEmailCodeCommand : IRequest<ResendChangeEmailCodeResponse>
    {
        public Guid UserId { get; init; }
    }
}
