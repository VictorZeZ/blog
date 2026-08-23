using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsByTag
{
    public class GetPostsByTagQuery : IRequest<PagedResult<PostSummaryResponse>>
    {
        public List<string> Tags { get; init; } = [];
        public PagedRequest Paging { get; init; } = new();
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
        public PostTagGroupingMode GroupingMode { get; init; } = PostTagGroupingMode.None;
    }
}
