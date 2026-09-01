using blog.Api.Common;
using blog.Application.Dashboard.Queries.GetDashboard;
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
    }
}
