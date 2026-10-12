using blog.Application.Users.Queries.GetUsersDailyReport;
using blog.Domain.Categories.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
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

            var myPosts = await postRepository.GetOverviewCountsAsync(actor.Id, cancellationToken);

            SiteOverviewResponse? site = null;
            if (actor.IsElevated())
            {
                var sitePosts = await postRepository.GetOverviewCountsAsync(null, cancellationToken);
                var users = await userRepository.GetOverviewCountsAsync(cancellationToken);
                var categories = await categoryRepository.GetOverviewCountsAsync(cancellationToken);

                site = new SiteOverviewResponse
                {
                    Posts = ToResponse(sitePosts),
                    Users = ToResponse(users),
                    Categories = ToResponse(categories)
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
                MyPosts = ToResponse(myPosts),
                Site = site
            };
        }

        private static PostOverviewResponse ToResponse(PostOverviewCounts counts) => new()
        {
            TotalCount = counts.DraftCount + counts.PendingApprovalCount + counts.PublishedCount + counts.RejectedCount,
            DraftCount = counts.DraftCount,
            PendingApprovalCount = counts.PendingApprovalCount,
            PublishedCount = counts.PublishedCount,
            RejectedCount = counts.RejectedCount,
            TotalViewCount = counts.TotalViewCount
        };

        private static UserOverviewResponse ToResponse(UserOverviewCounts counts)
        {
            var totalCount = counts.NormalCount + counts.AuthorCount + counts.AdminCount + counts.OwnerCount;

            return new UserOverviewResponse
            {
                TotalCount = totalCount,
                Levels = new UserLevelTotalsResponse
                {
                    NormalCount = counts.NormalCount,
                    AuthorCount = counts.AuthorCount,
                    AdminCount = counts.AdminCount,
                    OwnerCount = counts.OwnerCount
                },
                ActiveCount = counts.ActiveCount,
                BannedCount = counts.BannedCount,
                DeletedCount = counts.DeletedCount,
                EmailConfirmedCount = counts.EmailConfirmedCount,
                EmailUnconfirmedCount = totalCount - counts.EmailConfirmedCount,
                TwoFactorEnabledCount = counts.TwoFactorEnabledCount
            };
        }

        private static CategoryOverviewResponse ToResponse(CategoryOverviewCounts counts) => new()
        {
            TotalCount = counts.ActiveCount + counts.DeletedCount,
            ActiveCount = counts.ActiveCount,
            DeletedCount = counts.DeletedCount
        };
    }
}