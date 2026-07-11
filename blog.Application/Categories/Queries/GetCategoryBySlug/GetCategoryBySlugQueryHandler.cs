using blog.Domain.Categories.Repository;
using blog.Domain.Exceptions;
using MediatR;

namespace blog.Application.Categories.Queries.GetCategoryBySlug
{
    public class GetCategoryBySlugQueryHandler(ICategoryRepository categoryRepository) : IRequestHandler<GetCategoryBySlugQuery, GetCategoryBySlugResponse>
    {
        public async Task<GetCategoryBySlugResponse> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
        {
            var category = await categoryRepository.GetBySlugAsync(request.Slug, cancellationToken);
            if (category is null || category.IsDeleted)
                throw new NotFoundException("Category", request.Slug);

            return new GetCategoryBySlugResponse
            {
                Id = category.Id.Value,
                Name = category.Name,
                Slug = category.Slug
            };
        }
    }
}
