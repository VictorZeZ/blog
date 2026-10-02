using blog.Domain.Common.Enum;

namespace blog.Api.DTOs.Dashboard
{
    public class CategoriesDetailRequest : ReportDateRangeRequest
    {
        public TriStateFilter Deleted { get; init; } = TriStateFilter.All;
    }
}