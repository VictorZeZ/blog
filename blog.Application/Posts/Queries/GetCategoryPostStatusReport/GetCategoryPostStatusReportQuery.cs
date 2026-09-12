using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetCategoryPostStatusReport
{
    public class GetCategoryPostStatusReportQuery : IRequest<GetCategoryPostStatusReportResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public Guid CategoryId { get; init; }
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}