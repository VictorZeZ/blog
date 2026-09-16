using blog.Api.Common;
using blog.Application.Categories.Queries.GetCategoryPerformanceReport;
using blog.Application.Dashboard.Queries.GetDashboard;
using blog.Application.Posts.Queries.GetCategoryPostStatusReport;
using blog.Application.Posts.Queries.GetMyPostStatusReport;
using blog.Application.Posts.Queries.GetPostsReport;
using blog.Application.Posts.Queries.GetPostStatusReport;
using blog.Application.Posts.Queries.GetTopPosts;
using blog.Application.Posts.Queries.GetUserPostStatusReport;
using blog.Application.Users.Queries.GetMySessions;
using blog.Application.Users.Queries.GetTopAuthors;
using blog.Application.Users.Queries.GetUserActivityReport;
using blog.Domain.Common;
using blog.Domain.Common.Reports;
using blog.Domain.Posts.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace blog.Api.Controllers
{
    [Authorize]
    public class DashboardController(IMediator mediator) : ApiController(mediator)
    {
        [HttpGet]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
        {
            var query = new GetDashboardQuery { ActorId = CurrentUserId };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("posts/my-status-report")]
        public async Task<IActionResult> GetMyPostStatusReport([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        {
            var query = new GetMyPostStatusReportQuery
            {
                ActorId = CurrentUserId,
                From = from,
                To = to
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetMySessions(CancellationToken ct)
        {
            var query = new GetMySessionsQuery { ActorId = CurrentUserId };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("posts/status-report")]
        public async Task<IActionResult> GetPostStatusReport([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        {
            var query = new GetPostStatusReportQuery
            {
                ActorId = CurrentUserId,
                From = from,
                To = to
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("posts/{authorId:guid}/status-report")]
        public async Task<IActionResult> GetUserPostStatusReport(Guid authorId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        {
            var query = new GetUserPostStatusReportQuery
            {
                ActorId = CurrentUserId,
                AuthorId = authorId,
                From = from,
                To = to
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("categories/{categoryId:guid}/status-report")]
        public async Task<IActionResult> GetCategoryPostStatusReport(Guid categoryId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        {
            var query = new GetCategoryPostStatusReportQuery
            {
                ActorId = CurrentUserId,
                CategoryId = categoryId,
                From = from,
                To = to
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("categories/performance-report")]
        public async Task<IActionResult> GetCategoryPerformanceReport([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        {
            var query = new GetCategoryPerformanceReportQuery
            {
                ActorId = CurrentUserId,
                From = from,
                To = to
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("posts/report")]
        public async Task<IActionResult> GetPostsReport([FromQuery] PagedRequest paging, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] PostFilter filter = PostFilter.All, [FromQuery] PostSortBy sortBy = PostSortBy.Newest, [FromQuery] Guid? categoryId = null, [FromQuery] Guid? authorId = null, CancellationToken ct = default)
        {
            var query = new GetPostsReportQuery
            {
                ActorId = CurrentUserId,
                Paging = paging,
                From = from,
                To = to,
                Filter = filter,
                SortBy = sortBy,
                CategoryId = categoryId,
                AuthorId = authorId
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("posts/top")]
        public async Task<IActionResult> GetTopPosts([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int topN = TopNRules.DefaultTopN, [FromQuery] Guid? categoryId = null, CancellationToken ct = default)
        {
            var query = new GetTopPostsQuery
            {
                ActorId = CurrentUserId,
                From = from,
                To = to,
                TopN = topN,
                CategoryId = categoryId
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("users/top-authors")]
        public async Task<IActionResult> GetTopAuthors([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int topN = TopNRules.DefaultTopN, CancellationToken ct = default)
        {
            var query = new GetTopAuthorsQuery
            {
                ActorId = CurrentUserId,
                From = from,
                To = to,
                TopN = topN
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("users/activity-report")]
        public async Task<IActionResult> GetUserActivityReport([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
        {
            var query = new GetUserActivityReportQuery
            {
                ActorId = CurrentUserId,
                From = from,
                To = to
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }
    }
}