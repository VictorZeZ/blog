using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Users.Commands.BanUser
{
    public class BanUserCommand : IRequest<BanUserResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public Guid TargetUserId { get; init; }
        public bool IsBanned { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}
