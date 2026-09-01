using blog.Domain.Categories.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class GetDashboardQueryHandler(IUserRepository userRepository, IPostRepository postRepository, ICategoryRepository categoryRepository) : IRequestHandler<GetDashboardQuery, GetDashboardResponse>
    {
        private const int ChartDayCount = 30;

        public async Task<GetDashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            var myPostStats = await postRepository.GetStatsByAuthorAsync(actor.Id, ChartDayCount, cancellationToken);

            var profile = new DashboardProfileResponse
            {
                Id = actor.Id.Value,
                FullName = actor.FullName,
                Level = actor.Level,
                IsEmailConfirmed = actor.IsEmailConfirmed,
                TwoFactorEnabled = actor.TwoFactorEnabled,
                CreatedAt = actor.CreatedAt
            };

            var myContent = new MyContentResponse
            {
                DraftCount = myPostStats.DraftCount,
                PendingApprovalCount = myPostStats.PendingApprovalCount,
                PublishedCount = myPostStats.PublishedCount,
                RejectedCount = myPostStats.RejectedCount,
                TotalViewCount = myPostStats.TotalViewCount
            };

            var authorInsights = actor.IsAuthorOrHigher()
                ? new AuthorInsightsResponse { PostsPerDay = myPostStats.PostsPerDay }
                : null;

            if (!actor.IsElevated())
            {
                return new GetDashboardResponse
                {
                    Profile = profile,
                    MyContent = myContent,
                    AuthorInsights = authorInsights
                };
            }

            var platformPostStats = await postRepository.GetStatsAsync(ChartDayCount, cancellationToken);
            var userStats = await userRepository.GetStatsAsync(ChartDayCount, cancellationToken);
            var activeCategoryCount = await categoryRepository.GetActiveCountAsync(cancellationToken);

            var moderationQueue = new ModerationQueueResponse
            {
                PendingApprovalCount = platformPostStats.PendingApprovalCount,
                ActiveCategoryCount = activeCategoryCount
            };

            var platformStats = new PlatformStatsResponse
            {
                TotalUserCount = userStats.TotalCount,
                BannedUserCount = userStats.BannedCount,
                TotalPostCount = platformPostStats.TotalCount,
                TotalViewCount = platformPostStats.TotalViewCount,
                RegistrationsPerDay = userStats.RegistrationsPerDay
            };

            var ownerOverview = actor.Level == UserLevel.Owner
                ? new OwnerOverviewResponse
                {
                    NormalCount = userStats.NormalCount,
                    AuthorCount = userStats.AuthorCount,
                    AdminCount = userStats.AdminCount,
                    OwnerCount = userStats.OwnerCount
                }
                : null;

            return new GetDashboardResponse
            {
                Profile = profile,
                MyContent = myContent,
                AuthorInsights = authorInsights,
                ModerationQueue = moderationQueue,
                PlatformStats = platformStats,
                OwnerOverview = ownerOverview
            };
        }
    }
}