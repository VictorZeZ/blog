using blog.Domain.Categories.Common;
using blog.Domain.Categories.Enums;
using blog.Domain.Common;
using blog.Domain.Common.Enum;
using blog.Domain.Posts.Enums;
using MediatR;

namespace blog.Application.Categories.Queries.SearchCategories
{
    public class SearchCategoriesQuery : IRequest<PagedResult<CategoryWithPostCount>>
    {
        public string Term { get; init; } = string.Empty;
        public Guid? ActorId { get; init; }
        public PagedRequest Paging { get; init; } = new();
        public CategorySortBy SortBy { get; init; } = CategorySortBy.Newest;
        public TriStateFilter Deleted { get; init; } = TriStateFilter.All;
        public List<PostStatus> PostStatuses { get; init; } = [];
    }
}