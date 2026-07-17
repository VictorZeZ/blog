using blog.Api.Common;
using blog.Api.DTOs.Users;
using blog.Application.Users.Commands.ConfirmEmail;
using blog.Application.Users.Commands.ConfirmLogin;
using blog.Application.Users.Commands.Login;
using blog.Application.Users.Commands.Logout;
using blog.Application.Users.Commands.RefreshToken;
using blog.Application.Users.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace blog.Api.Controllers
{
    public class AuthController(IMediator mediator) : ApiController(mediator)
    {
        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var command = new RegisterCommand
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Password = request.Password
            };

            var result = await Mediator.Send(command, ct);
            return CreatedAtAction(nameof(Register), result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            var command = new LoginCommand
            {
                Email = request.Email,
                Password = request.Password,
                DeviceInfo = Request.Headers.UserAgent.ToString()
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken ct)
        {
            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
        {
            var command = new RefreshTokenCommand
            {
                RefreshToken = request.RefreshToken,
                DeviceInfo = Request.Headers.UserAgent.ToString()
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
        {
            var command = new ConfirmEmailCommand
            {
                Email = request.Email,
                Code = request.Code,
                DeviceInfo = Request.Headers.UserAgent.ToString()
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }

        [HttpPost("confirm-login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<IActionResult> ConfirmLogin([FromBody] ConfirmLoginRequest request, CancellationToken ct)
        {
            var command = new ConfirmLoginCommand
            {
                ChallengeId = request.ChallengeId,
                Code = request.Code,
                DeviceInfo = Request.Headers.UserAgent.ToString()
            };

            var result = await Mediator.Send(command, ct);
            return Ok(result);
        }
    }
}
