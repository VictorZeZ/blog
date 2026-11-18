using blog.Domain.Categories.Repository;
using blog.Domain.Common;
using MediatR;

namespace blog.Application.Categories.Queries.GetCategories
{
    public class GetCategoriesQueryHandler(ICategoryRepository categoryRepository) : IRequestHandler<GetCategoriesQuery, PagedResult<GetCategoriesResponse>>
    {
        public async Task<PagedResult<GetCategoriesResponse>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var result = await categoryRepository.GetAllWithPostCountAsync(
                request.Paging,
                request.SortBy,
                request.Deleted,
                request.PostStatuses,
                cancellationToken);

            return new PagedResult<GetCategoriesResponse>(
                result.Items.Select(c => new GetCategoriesResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    IsDeleted = c.IsDeleted,
                    DeletedAt = c.DeletedAt,
                    CreatedAt = c.CreatedAt,
                    PostsCount = c.PostsCount
                }),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }
    }
}