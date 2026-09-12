using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Reports;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Extensions;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.GetTopPosts
{
    public class GetTopPostsQueryHandler(IPostRepository postRepository, ICategoryRepository categoryRepository) : IRequestHandler<GetTopPostsQuery, IReadOnlyList<PostSummaryResponse>>
    {
        public async Task<IReadOnlyList<PostSummaryResponse>> Handle(GetTopPostsQuery request, CancellationToken cancellationToken)
        {
            CategoryId? categoryId = null;
            if (request.CategoryId is not null)
            {
                categoryId = new CategoryId(request.CategoryId.Value);
                var category = await categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
                if (category is null)
                    throw new NotFoundException("Category", request.CategoryId.Value);
            }

            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var posts = await postRepository.GetTopViewedAsync(range.From, range.To, request.TopN, categoryId, cancellationToken);

            return posts.Select(p => p.ToSummaryResponse()).ToList();
        }
    }
}