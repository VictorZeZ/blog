using blog.Domain.Categories.Repository;
using blog.Domain.Common;
using blog.Domain.Common.Reports;
using MediatR;

namespace blog.Application.Categories.Queries.GetCategoriesDetail
{
    public class GetCategoriesDetailQueryHandler(ICategoryRepository categoryRepository) : IRequestHandler<GetCategoriesDetailQuery, PagedResult<CategoryDetailResponse>>
    {
        public async Task<PagedResult<CategoryDetailResponse>> Handle(GetCategoriesDetailQuery request, CancellationToken cancellationToken)
        {
            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var result = await categoryRepository.GetPagedAsync(request.Paging, range.From, range.To, request.Deleted, cancellationToken);

            return new PagedResult<CategoryDetailResponse>(
                result.Items.Select(c => new CategoryDetailResponse
                {
                    Id = c.Id.Value,
                    Name = c.Name,
                    Slug = c.Slug,
                    IsDeleted = c.IsDeleted,
                    DeletedAt = c.DeletedAt,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }
    }
}