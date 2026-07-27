using MediatR;

namespace blog.Application.Users.Commands.ResendLoginVerificationCode
{
    public class ResendLoginVerificationCodeCommand : IRequest<ResendLoginVerificationCodeResponse>
    {
        public Guid ChallengeId { get; init; }
    }
}
