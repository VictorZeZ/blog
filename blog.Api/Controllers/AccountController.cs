using blog.Api.Common;
using blog.Api.DTOs.Users;
using blog.Application.Users.Commands.ChangeEmail;
using blog.Application.Users.Commands.ChangePassword;
using blog.Application.Users.Commands.ConfirmChangeEmail;
using blog.Application.Users.Commands.ConfirmNewEmail;
using blog.Application.Users.Commands.DeleteAccount;
using blog.Application.Users.Commands.TwoFactor;
using blog.Application.Users.Commands.UpdateUser;
using blog.Application.Users.Queries.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace blog.Api.Controllers
{
    [Authorize]
    public class AccountController(IMediator mediator) : ApiController(mediator)
    {
        [HttpGet("me")]
        public async Task<IActionResult> GetUserById(CancellationToken ct)
        {
            var query = new GetUserByIdQuery { UserId = CurrentUserId };

            var result = await Mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request, CancellationToken ct)
        {
            var command = new UpdateUserCommand
            {
                UserId = CurrentUserId,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            var command = new ChangePasswordCommand
            {
                UserId = CurrentUserId,
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPut("me/two-factor")]
        public async Task<IActionResult> TwoFactor([FromBody] TwoFactorRequest request, CancellationToken ct)
        {
            var command = new TwoFactorCommand
            {
                UserId = CurrentUserId,
                TwoFactor = request.TwoFactor
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }
        [HttpPost("me/change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request, CancellationToken ct)
        {
            var command = new ChangeEmailCommand
            {
                UserId = CurrentUserId,
                NewEmail = request.NewEmail,
                CurrentPassword = request.CurrentPassword
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPost("me/change-email/confirm")]
        public async Task<IActionResult> ConfirmChangeEmail([FromBody] ConfirmChangeEmailRequest request, CancellationToken ct)
        {
            var command = new ConfirmChangeEmailCommand
            {
                UserId = CurrentUserId,
                Code = request.Code
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPost("me/change-email/confirm-new")]
        public async Task<IActionResult> ConfirmNewEmail([FromBody] ConfirmNewEmailRequest request, CancellationToken ct)
        {
            var command = new ConfirmNewEmailCommand
            {
                UserId = CurrentUserId,
                Code = request.Code
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request, CancellationToken ct)
        {
            var command = new DeleteAccountCommand
            {
                UserId = CurrentUserId,
                CurrentPassword = request.CurrentPassword
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }
    }
}
