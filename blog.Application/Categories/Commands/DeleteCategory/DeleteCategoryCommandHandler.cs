using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using MediatR;

namespace blog.Application.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteCategoryCommand, DeleteCategoryResponse>
    {
        public async Task<DeleteCategoryResponse> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await categoryRepository.GetByIdAsync(new CategoryId(request.CategoryId), cancellationToken);
            if (category is null)
                throw new NotFoundException("Category", request.CategoryId);

            if (category.IsDeleted)
                throw new InvalidStateException("Category", "Deleted", "Active");

            categoryRepository.SoftDelete(category);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteCategoryResponse { Success = true };
        }
    }
}
