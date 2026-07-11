using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using MediatR;

namespace blog.Application.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateCategoryCommand, UpdateCategoryResponse>
    {
        public async Task<UpdateCategoryResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await categoryRepository.GetByIdAsync(new CategoryId(request.CategoryId), cancellationToken);
            if (category is null)
                throw new NotFoundException("Category", request.CategoryId);

            if (!category.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
            {
                var nameTaken = await categoryRepository.ExistsByNameAsync(request.Name, cancellationToken);
                if (nameTaken)
                    throw new AlreadyExistsException("Category", request.Name);
            }

            category.Update(request.Name);

            categoryRepository.Update(category);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateCategoryResponse
            {
                Id = category.Id.Value,
                Name = category.Name,
                Slug = category.Slug
            };
        }
    }
}
