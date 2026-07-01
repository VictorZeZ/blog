using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsByAuthor
{
    public class GetPostsByAuthorQuery : IRequest<PagedResult<PostSummaryResponse>>
    {
        public Guid AuthorId { get; init; }
        public Guid? ActorId { get; init; }
        public PagedRequest Paging { get; init; } = new();
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
    }
}
