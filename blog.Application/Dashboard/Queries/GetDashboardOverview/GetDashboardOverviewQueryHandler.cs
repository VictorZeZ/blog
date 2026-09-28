using blog.Domain.Categories.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class GetDashboardOverviewQueryHandler(IUserRepository userRepository, IPostRepository postRepository, ICategoryRepository categoryRepository) : IRequestHandler<GetDashboardOverviewQuery, GetDashboardOverviewResponse>
    {
        public async Task<GetDashboardOverviewResponse> Handle(GetDashboardOverviewQuery request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            var myPostsCount = await postRepository.CountAsync(request.PostFilter, actor.Id, cancellationToken);

            SiteOverviewResponse? site = null;
            if (actor.IsElevated())
            {
                site = new SiteOverviewResponse
                {
                    PostsCount = await postRepository.CountAsync(request.PostFilter, null, cancellationToken),
                    UsersCount = await userRepository.CountAsync(
                        request.UserLevelFilter,
                        request.UserBanned,
                        request.UserDeleted,
                        request.UserEmailConfirmed,
                        request.UserTwoFactorEnabled,
                        cancellationToken),
                    CategoriesCount = await categoryRepository.CountAsync(request.CategoryDeleted, cancellationToken)
                };
            }

            return new GetDashboardOverviewResponse
            {
                Profile = new DashboardProfileResponse
                {
                    Id = actor.Id.Value,
                    FullName = actor.FullName,
                    Level = actor.Level,
                    IsEmailConfirmed = actor.IsEmailConfirmed,
                    TwoFactorEnabled = actor.TwoFactorEnabled,
                    CreatedAt = actor.CreatedAt
                },
                MyPostsCount = myPostsCount,
                Site = site
            };
        }
    }
}