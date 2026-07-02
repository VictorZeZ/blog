using blog.Api.Common;
using blog.Api.DTOs.Posts;
using blog.Api.DTOs.Users;
using blog.Application.Posts.Commands.ChangePostStatus;
using blog.Application.Posts.Queries.GetAllPosts;
using blog.Application.Posts.Queries.GetPendingApprovalPosts;
using blog.Application.Users.Commands.BanUser;
using blog.Application.Users.Commands.ChangeUserLevel;
using blog.Application.Users.Queries.GetUsers;
using blog.Domain.Common;
using blog.Domain.Posts.Enums;
using blog.Domain.Users.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace blog.Api.Controllers
{
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

        [HttpGet("posts")]
        public async Task<IActionResult> GetAllPosts([FromQuery] PagedRequest paging, [FromQuery] PostSortBy sortBy = PostSortBy.Newest, [FromQuery] PostFilter filter = PostFilter.All, CancellationToken ct = default)
        {
            var query = new GetAllPostsQuery
            {
                ActorId = CurrentUserId,
                Paging = paging,
                SortBy = sortBy,
                Filter = filter
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("posts/pending")]
        public async Task<IActionResult> GetPendingApprovalPosts([FromQuery] PagedRequest paging, [FromQuery] PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = new GetPendingApprovalPostsQuery
            {
                ActorId = CurrentUserId,
                Paging = paging,
                SortBy = sortBy
            };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpPut("posts/{postId:guid}/status")]
        public async Task<IActionResult> ChangePostStatus(Guid postId, [FromBody] ChangePostStatusRequest request, CancellationToken ct)
        {
            var command = new ChangePostStatusCommand
            {
                ActorId = CurrentUserId,
                PostId = postId,
                Action = request.Action
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }
    }
}
