using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.SearchPosts
{
    public class SearchPostsQueryHandler(IPostRepository postRepository) : IRequestHandler<SearchPostsQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(SearchPostsQuery request, CancellationToken cancellationToken)
        {
            return await postRepository.SearchAsync(request.Paging, request.Term, request.SortBy, cancellationToken);
        }
    }
}