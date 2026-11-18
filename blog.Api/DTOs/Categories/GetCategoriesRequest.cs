using blog.Domain.Categories.Enums;
using blog.Domain.Common.Enum;
using blog.Domain.Posts.Enums;

namespace blog.Api.DTOs.Categories
{
    public class GetCategoriesRequest
    {
        public CategorySortBy SortBy { get; init; } = CategorySortBy.Newest;
        public TriStateFilter Deleted { get; init; } = TriStateFilter.All;
        public List<PostStatus> PostStatuses { get; init; } = [];
    }
}