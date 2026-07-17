using MediatR;

namespace blog.Application.Users.Commands.ConfirmLogin
{
    public class ConfirmLoginCommand : IRequest<ConfirmLoginResponse>
    {
        public Guid ChallengeId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string DeviceInfo { get; init; } = string.Empty;
    }
}
