using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostBySlug
{
    public class GetPostBySlugQueryValidator : AbstractValidator<GetPostBySlugQuery>
    {
        public GetPostBySlugQueryValidator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty();
        }
    }
}
