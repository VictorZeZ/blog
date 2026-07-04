using blog.Domain.Common;
using blog.Domain.Common.Interfaces;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetAllPosts
{
    public class GetAllPostsQuery : IRequest<PagedResult<PostSummaryResponse>>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public PagedRequest Paging { get; init; } = new();
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
        public PostFilter Filter { get; init; } = PostFilter.All;

        public UserLevel MinimumLevel => UserLevel.Owner;
    }
}
