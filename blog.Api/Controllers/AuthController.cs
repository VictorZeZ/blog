using blog.Api.Common;
using blog.Api.DTOs.Users;
using blog.Application.Users.Commands.Login;
using blog.Application.Users.Commands.Logout;
using blog.Application.Users.Commands.RefreshToken;
using blog.Application.Users.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace blog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IMediator mediator) : ApiController(mediator)
    {
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var command = new RegisterCommand
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Password = request.Password,
                DeviceInfo = Request.Headers.UserAgent.ToString()
            };

            var result = await Mediator.Send(command, ct);
            return CreatedAtAction(nameof(Register), result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
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
    }
}
