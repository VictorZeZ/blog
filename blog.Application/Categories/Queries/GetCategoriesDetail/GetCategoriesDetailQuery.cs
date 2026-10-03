using blog.Domain.Categories.Enums;
using blog.Domain.Common;
using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Categories.Queries.GetCategoriesDetail
{
    public class GetCategoriesDetailQuery : IRequest<PagedResult<CategoryDetailResponse>>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public PagedRequest Paging { get; init; } = new();
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
        public TriStateFilter Deleted { get; init; } = TriStateFilter.All;
        public CategorySortBy SortBy { get; init; } = CategorySortBy.Newest;

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}