using MediatR;

namespace blog.Application.Users.Commands.ConfirmEmail
{
    public class ConfirmEmailCommand : IRequest<ConfirmEmailResponse>
    {
        public string Email { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string DeviceInfo { get; init; } = string.Empty;
    }
}
