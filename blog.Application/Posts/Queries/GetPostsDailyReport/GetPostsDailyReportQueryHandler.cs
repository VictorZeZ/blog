using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Reports;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsDailyReport
{
    public class GetPostsDailyReportQueryHandler(IPostRepository postRepository, ICategoryRepository categoryRepository, IUserRepository userRepository) : IRequestHandler<GetPostsDailyReportQuery, PostsDailyReportResponse>
    {
        public async Task<PostsDailyReportResponse> Handle(GetPostsDailyReportQuery request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            var authorId = await userRepository.ResolveAuthorScopeAsync(actor, request.AuthorId, cancellationToken);

            CategoryId? categoryId = null;
            if (request.CategoryId is not null)
            {
                categoryId = new CategoryId(request.CategoryId.Value);
                var category = await categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
                if (category is null)
                    throw new NotFoundException("Category", request.CategoryId.Value);
            }

            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var days = await postRepository.GetDailyStatusReportAsync(range.From, range.To, categoryId, authorId, cancellationToken);

            return new PostsDailyReportResponse
            {
                From = range.From,
                To = range.To,
                CategoryId = categoryId?.Value,
                AuthorId = authorId?.Value,
                TotalCount = days.Sum(d => d.DraftCount + d.PendingApprovalCount + d.PublishedCount + d.RejectedCount),
                StatusTotals = new PostStatusTotalsResponse
                {
                    DraftCount = days.Sum(d => d.DraftCount),
                    PendingApprovalCount = days.Sum(d => d.PendingApprovalCount),
                    PublishedCount = days.Sum(d => d.PublishedCount),
                    RejectedCount = days.Sum(d => d.RejectedCount)
                },
                Days = days.Select(d => new PostDailyCountResponse
                {
                    Date = d.Date,
                    DraftCount = d.DraftCount,
                    PendingApprovalCount = d.PendingApprovalCount,
                    PublishedCount = d.PublishedCount,
                    RejectedCount = d.RejectedCount,
                    TotalCount = d.DraftCount + d.PendingApprovalCount + d.PublishedCount + d.RejectedCount
                }).ToList()
            };
        }
    }
}