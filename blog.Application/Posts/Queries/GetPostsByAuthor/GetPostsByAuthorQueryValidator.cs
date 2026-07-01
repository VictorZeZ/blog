using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostsByAuthor
{
    public class GetPostsByAuthorQueryValidator : AbstractValidator<GetPostsByAuthorQuery>
    {
        public GetPostsByAuthorQueryValidator()
        {
            RuleFor(x => x.AuthorId).NotEmpty();
            RuleFor(x => x.Paging.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Paging.PageSize).GreaterThanOrEqualTo(1);
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}
