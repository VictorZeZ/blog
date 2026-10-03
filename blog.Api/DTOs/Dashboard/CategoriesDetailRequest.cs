using blog.Domain.Categories.Enums;
using blog.Domain.Common.Enum;

namespace blog.Api.DTOs.Dashboard
{
    public class CategoriesDetailRequest : ReportDateRangeRequest
    {
        public TriStateFilter Deleted { get; init; } = TriStateFilter.All;
        public CategorySortBy SortBy { get; init; } = CategorySortBy.Newest;
    }
}