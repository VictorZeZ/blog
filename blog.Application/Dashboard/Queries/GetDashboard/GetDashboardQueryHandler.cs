using blog.Domain.Categories.Repository;
using blog.Domain.Common;
using blog.Domain.Common.Reports;
using blog.Domain.Dashboard.Enums;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Extensions;
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
        // Used only to source the all-time TotalViewCount snapshots below; the daily
        // breakdowns these calls also compute are discarded in favor of the range-scoped
        // status reports, which is the deliberate trade-off documented alongside this commit.
        private const int AllTimeStatsDayCount = 30;

        public async Task<GetDashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var myPostStats = await postRepository.GetStatsByAuthorAsync(actor.Id, AllTimeStatsDayCount, cancellationToken);
            var myStatusReport = await postRepository.GetStatusReportByAuthorAsync(actor.Id, range.From, range.To, cancellationToken);

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
                TotalViewCount = myPostStats.TotalViewCount,
                DailyBreakdown = myStatusReport.DailyBreakdown
            };

            var authorInsights = actor.IsAuthorOrHigher()
                ? new AuthorInsightsResponse
                {
                    PostsPerDay = myStatusReport.DailyBreakdown
                        .Select(d => new DailyCount(d.Date, d.DraftCount + d.PendingApprovalCount + d.PublishedCount + d.RejectedCount))
                        .ToList()
                }
                : null;

            if (!actor.IsElevated() || request.Scope == DashboardScope.Personal)
            {
                return new GetDashboardResponse
                {
                    From = range.From,
                    To = range.To,
                    Profile = profile,
                    MyContent = myContent,
                    AuthorInsights = authorInsights
                };
            }

            var platformPostStats = await postRepository.GetStatsAsync(AllTimeStatsDayCount, cancellationToken);
            var siteStatusReport = await postRepository.GetStatusReportAsync(range.From, range.To, cancellationToken);
            var userStats = await userRepository.GetStatsAsync(AllTimeStatsDayCount, cancellationToken);
            var registrationsPerDay = await userRepository.GetRegistrationsPerDayAsync(range.From, range.To, cancellationToken);
            var activeCategoryCount = await categoryRepository.GetActiveCountAsync(cancellationToken);

            var topPosts = await postRepository.GetTopViewedAsync(range.From, range.To, TopNRules.DefaultTopN, null, cancellationToken);
            var topAuthors = await postRepository.GetTopAuthorsAsync(range.From, range.To, TopNRules.DefaultTopN, cancellationToken);

            var moderationQueue = new ModerationQueueResponse
            {
                PendingApprovalCount = platformPostStats.PendingApprovalCount,
                ActiveCategoryCount = activeCategoryCount
            };

            var siteContent = new SiteContentResponse
            {
                DraftCount = platformPostStats.DraftCount,
                PendingApprovalCount = platformPostStats.PendingApprovalCount,
                PublishedCount = platformPostStats.PublishedCount,
                RejectedCount = platformPostStats.RejectedCount,
                TotalViewCount = platformPostStats.TotalViewCount,
                DailyBreakdown = siteStatusReport.DailyBreakdown
            };

            var platformStats = new PlatformStatsResponse
            {
                TotalUserCount = userStats.TotalCount,
                BannedUserCount = userStats.BannedCount,
                TotalPostCount = platformPostStats.TotalCount,
                TotalViewCount = platformPostStats.TotalViewCount,
                RegistrationsPerDay = registrationsPerDay,
                TopPosts = topPosts.Select(p => p.ToSummaryResponse()).ToList(),
                TopAuthors = topAuthors
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
                From = range.From,
                To = range.To,
                Profile = profile,
                MyContent = myContent,
                AuthorInsights = authorInsights,
                ModerationQueue = moderationQueue,
                SiteContent = siteContent,
                PlatformStats = platformStats,
                OwnerOverview = ownerOverview
            };
        }
    }
}