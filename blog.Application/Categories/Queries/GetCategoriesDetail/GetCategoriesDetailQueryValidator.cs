using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Categories.Queries.GetCategoriesDetail
{
    public class GetCategoriesDetailQueryValidator : AbstractValidator<GetCategoriesDetailQuery>
    {
        public GetCategoriesDetailQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.Paging.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Paging.PageSize)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Deleted)
                .IsInEnum();

            RuleFor(x => x.SortBy)
                .IsInEnum();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}