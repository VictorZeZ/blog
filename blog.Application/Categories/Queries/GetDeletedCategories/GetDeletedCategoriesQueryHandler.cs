using blog.Domain.Categories.Repository;
using MediatR;

namespace blog.Application.Categories.Queries.GetDeletedCategories
{
    public class GetDeletedCategoriesQueryHandler(ICategoryRepository categoryRepository) : IRequestHandler<GetDeletedCategoriesQuery, IEnumerable<GetDeletedCategoriesResponse>>
    {
        public async Task<IEnumerable<GetDeletedCategoriesResponse>> Handle(GetDeletedCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await categoryRepository.GetAllDeletedAsync(cancellationToken);

            return categories.Select(c => new GetDeletedCategoriesResponse
            {
                Id = c.Id.Value,
                Name = c.Name,
                Slug = c.Slug,
                DeletedAt = c.DeletedAt
            });
        }
    }
}