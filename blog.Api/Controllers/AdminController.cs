using blog.Api.Common;
using blog.Api.DTOs.Users;
using blog.Application.Users.Commands.BanUser;
using blog.Application.Users.Commands.ChangeUserLevel;
using blog.Application.Users.Queries.GetUsers;
using blog.Domain.Common;
using blog.Domain.Users.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace blog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController(IMediator mediator) : ApiController(mediator)
    {
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] PagedRequest paging, [FromQuery] UserSortBy sortBy = UserSortBy.Newest, [FromQuery] UserFilter filter = UserFilter.All, CancellationToken ct = default)
        {
            var query = new GetUsersQuery
            {
                ActorId = CurrentUserId,
                Paging = paging,
                SortBy = sortBy,
                Filter = filter
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpPut("users/{targetUserId:guid}/ban")]
        public async Task<IActionResult> BanUser(Guid targetUserId, [FromBody] BanUserRequest request, CancellationToken ct)
        {
            var command = new BanUserCommand
            {
                ActorId = CurrentUserId,
                TargetUserId = targetUserId,
                IsBanned = request.IsBanned
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPut("users/{targetUserId:guid}/level")]
        public async Task<IActionResult> ChangeUserLevel(Guid targetUserId, [FromBody] ChangeUserLevelRequest request, CancellationToken ct)
        {
            var command = new ChangeUserLevelCommand
            {
                ActorId = CurrentUserId,
                TargetUserId = targetUserId,
                Level = request.Level
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }
    }
}
