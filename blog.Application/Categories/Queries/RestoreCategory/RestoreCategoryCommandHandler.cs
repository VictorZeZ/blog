using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using MediatR;

namespace blog.Application.Categories.Commands.RestoreCategory
{
    public class RestoreCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork) : IRequestHandler<RestoreCategoryCommand, RestoreCategoryResponse>
    {
        public async Task<RestoreCategoryResponse> Handle(RestoreCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await categoryRepository.GetByIdAsync(new CategoryId(request.CategoryId), cancellationToken);
            if (category is null)
                throw new NotFoundException("Category", request.CategoryId);

            if (!category.IsDeleted)
                throw new InvalidStateException("Category", "Active", "Deleted");

            categoryRepository.Restore(category);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RestoreCategoryResponse
            {
                Id = category.Id.Value,
                Name = category.Name,
                Slug = category.Slug
            };
        }
    }
}