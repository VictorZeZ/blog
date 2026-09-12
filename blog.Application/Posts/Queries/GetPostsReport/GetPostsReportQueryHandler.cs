using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common;
using blog.Domain.Common.Reports;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Extensions;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsReport
{
    public class GetPostsReportQueryHandler(IPostRepository postRepository, ICategoryRepository categoryRepository, IUserRepository userRepository) : IRequestHandler<GetPostsReportQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetPostsReportQuery request, CancellationToken cancellationToken)
        {
            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            CategoryId? categoryId = null;
            if (request.CategoryId is not null)
            {
                categoryId = new CategoryId(request.CategoryId.Value);
                var category = await categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
                if (category is null)
                    throw new NotFoundException("Category", request.CategoryId.Value);
            }

            UserId? authorId = null;
            if (request.AuthorId is not null)
            {
                authorId = new UserId(request.AuthorId.Value);
                var author = await userRepository.GetByIdAsync(authorId.Value, cancellationToken);
                if (author is null)
                    throw new NotFoundException("User", request.AuthorId.Value);
            }

            var result = await postRepository.GetReportAsync(request.Paging, range.From, range.To, request.Filter, request.SortBy, categoryId, authorId, cancellationToken);

            return new PagedResult<PostSummaryResponse>(
                result.Items.Select(p => p.ToSummaryResponse()),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }
    }
}