using blog.Api.Common;
using blog.Api.DTOs.Dashboard;
using blog.Application.Dashboard.Queries.GetDashboardOverview;
using blog.Application.Users.Commands.RevokeSession;
using blog.Application.Users.Queries.GetMySessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace blog.Api.Controllers
{
    [Authorize]
    public class DashboardController(IMediator mediator) : ApiController(mediator)
    {
        [HttpGet]
        public async Task<IActionResult> GetDashboardOverview([FromQuery] DashboardOverviewRequest request, CancellationToken ct)
        {
            var query = new GetDashboardOverviewQuery
            {
                ActorId = CurrentUserId,
                PostFilter = request.PostFilter,
                UserLevelFilter = request.UserLevelFilter,
                UserBanned = request.UserBanned,
                UserDeleted = request.UserDeleted,
                UserEmailConfirmed = request.UserEmailConfirmed,
                UserTwoFactorEnabled = request.UserTwoFactorEnabled,
                CategoryDeleted = request.CategoryDeleted
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

        [HttpDelete("sessions/{sessionId:guid}")]
        public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
        {
            var command = new RevokeSessionCommand
            {
                ActorId = CurrentUserId,
                SessionId = sessionId
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }
    }
}