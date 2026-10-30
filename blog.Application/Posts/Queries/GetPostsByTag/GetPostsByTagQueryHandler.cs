using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsByTag
{
    public class GetPostsByTagQueryHandler(IPostRepository postRepository) : IRequestHandler<GetPostsByTagQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetPostsByTagQuery request, CancellationToken cancellationToken)
        {
            return await postRepository.GetByTagAsync(request.Paging, request.Tags, request.SortBy, request.GroupingMode, cancellationToken);
        }
    }
}