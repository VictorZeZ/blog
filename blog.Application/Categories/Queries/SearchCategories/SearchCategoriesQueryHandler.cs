using blog.Domain.Categories.Common;
using blog.Domain.Categories.Repository;
using blog.Domain.Common;
using blog.Domain.Common.Enum;
using blog.Domain.Posts.Enums;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using MediatR;

namespace blog.Application.Categories.Queries.SearchCategories
{
    public class SearchCategoriesQueryHandler(ICategoryRepository categoryRepository, IUserRepository userRepository) : IRequestHandler<SearchCategoriesQuery, PagedResult<CategoryWithPostCount>>
    {
        private static readonly PostStatus[] PublicPostStatuses = [PostStatus.Published];

        public async Task<PagedResult<CategoryWithPostCount>> Handle(SearchCategoriesQuery request, CancellationToken cancellationToken)
        {
            var isElevatedActor = await userRepository.IsElevatedActorAsync(request.ActorId, cancellationToken);

            var deleted = isElevatedActor ? request.Deleted : TriStateFilter.No;
            IReadOnlyCollection<PostStatus> postStatuses = isElevatedActor ? request.PostStatuses : PublicPostStatuses;

            return await categoryRepository.GetAllWithPostCountAsync(
                request.Paging,
                request.Term,
                request.SortBy,
                deleted,
                postStatuses,
                cancellationToken);
        }
    }
}