using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostStatusReport
{
    public class GetPostStatusReportQuery : IRequest<GetPostStatusReportResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}