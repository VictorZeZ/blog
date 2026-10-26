using blog.Domain.Common;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostsByAuthor
{
    public class GetPostsByAuthorQueryValidator : AbstractValidator<GetPostsByAuthorQuery>
    {
        public GetPostsByAuthorQueryValidator()
        {
            RuleFor(x => x.AuthorId).NotEmpty();
            RuleFor(x => x.Paging).SetValidator(new PagedRequestValidator());
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}