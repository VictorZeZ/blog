using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetUserPostStatusReport
{
    public class GetUserPostStatusReportQuery : IRequest<GetUserPostStatusReportResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public Guid AuthorId { get; init; }
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}
