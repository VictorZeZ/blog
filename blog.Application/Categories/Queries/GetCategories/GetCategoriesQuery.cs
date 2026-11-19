using blog.Domain.Categories.Common;
using blog.Domain.Categories.Enums;
using blog.Domain.Common;
using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Posts.Enums;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Categories.Queries.GetCategories
{
    public class GetCategoriesQuery : IRequest<PagedResult<CategoryWithPostCount>>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public string? Term { get; init; }
        public PagedRequest Paging { get; init; } = new();
        public CategorySortBy SortBy { get; init; } = CategorySortBy.Newest;
        public TriStateFilter Deleted { get; init; } = TriStateFilter.All;
        public List<PostStatus> PostStatuses { get; init; } = [];

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}