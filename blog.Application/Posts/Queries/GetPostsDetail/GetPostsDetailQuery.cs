using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsDetail
{
    public class GetPostsDetailQuery : IRequest<PagedResult<PostSummaryResponse>>
    {
        public Guid ActorId { get; init; }
        public PagedRequest Paging { get; init; } = new();
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
        public PostFilter Filter { get; init; } = PostFilter.All;
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
        public Guid? CategoryId { get; init; }
        public Guid? AuthorId { get; init; }
    }
}