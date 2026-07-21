using blog.Api.Common;
using blog.Application.Users.Queries.SearchUsers;
using blog.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace blog.Api.Controllers
{
    public class UsersController(IMediator mediator) : ApiController(mediator)
    {
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchUsers([FromQuery] string term, [FromQuery] PagedRequest paging, CancellationToken ct)
        {
            var query = new SearchUsersQuery
            {
                Term = term,
                ActorId = CurrentUserIdOrNull,
                Paging = paging
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }
    }
}
