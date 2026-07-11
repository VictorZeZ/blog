using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using MediatR;

namespace blog.Application.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateCategoryCommand, CreateCategoryResponse>
    {
        public async Task<CreateCategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var exists = await categoryRepository.ExistsByNameAsync(request.Name, cancellationToken);
            if (exists)
                throw new AlreadyExistsException("Category", request.Name);

            var category = new Category(request.Name);

            await categoryRepository.AddAsync(category, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateCategoryResponse
            {
                Id = category.Id.Value,
                Name = category.Name,
                Slug = category.Slug
            };
        }
    }
}
