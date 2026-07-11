using blog.Domain.Categories.Repository;
using MediatR;

namespace blog.Application.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQueryHandler(ICategoryRepository categoryRepository) : IRequestHandler<GetAllCategoriesQuery, IEnumerable<GetAllCategoriesResponse>>
    {
        public async Task<IEnumerable<GetAllCategoriesResponse>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await categoryRepository.GetAllActiveAsync(cancellationToken);

            return categories.Select(c => new GetAllCategoriesResponse
            {
                Id = c.Id.Value,
                Name = c.Name,
                Slug = c.Slug
            });
        }
    }
}
