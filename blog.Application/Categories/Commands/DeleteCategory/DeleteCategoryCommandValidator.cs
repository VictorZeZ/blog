using FluentValidation;

namespace blog.Application.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
    {
        public DeleteCategoryCommandValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.CategoryId)
                .NotEmpty();
        }
    }
}
