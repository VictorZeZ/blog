using blog.Domain.Categories.Common;
using blog.Domain.Categories.Repository;
using blog.Domain.Common;
using MediatR;

namespace blog.Application.Categories.Queries.GetCategories
{
    public class GetCategoriesQueryHandler(ICategoryRepository categoryRepository) : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryWithPostCount>>
    {
        public async Task<PagedResult<CategoryWithPostCount>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            return await categoryRepository.GetAllWithPostCountAsync(
                request.Paging,
                request.Term,
                request.SortBy,
                request.Deleted,
                request.PostStatuses,
                cancellationToken);
        }
    }
}