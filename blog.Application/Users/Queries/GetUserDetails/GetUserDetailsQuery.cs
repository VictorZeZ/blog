using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Users.Queries.GetUserDetails
{
    public class GetUserDetailsQuery : IRequest<GetUserDetailsResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public Guid TargetUserId { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}