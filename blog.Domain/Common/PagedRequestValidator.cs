using FluentValidation;

namespace blog.Domain.Common
{
    public class PagedRequestValidator : AbstractValidator<PagedRequest>
    {
        public PagedRequestValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Page)
                .Must((paging, page) => ((long)page - 1) * paging.PageSize <= int.MaxValue)
                    .WithMessage("Page is out of range.");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1);
        }
    }
}