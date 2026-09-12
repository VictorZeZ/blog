using blog.Domain.Common;
using blog.Domain.Common.Interfaces;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsReport
{
    public class GetPostsReportQuery : IRequest<PagedResult<PostSummaryResponse>>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public PagedRequest Paging { get; init; } = new();
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
        public PostFilter Filter { get; init; } = PostFilter.All;
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
        public Guid? CategoryId { get; init; }
        public Guid? AuthorId { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}