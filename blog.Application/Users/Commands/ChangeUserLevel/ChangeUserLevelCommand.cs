using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Users.Commands.ChangeUserLevel
{
    public class ChangeUserLevelCommand : IRequest<ChangeUserLevelResponse>
    {
        public Guid ActorId { get; init; }
        public Guid TargetUserId { get; init; }
        public UserLevel Level { get; init; }
    }
}
