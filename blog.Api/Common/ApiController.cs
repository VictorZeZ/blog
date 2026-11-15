using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace blog.Api.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class ApiController(IMediator mediator) : ControllerBase
    {
        protected readonly IMediator Mediator = mediator;

        protected Guid CurrentUserId =>
            Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        protected Guid? CurrentUserIdOrNull =>
            User.Identity?.IsAuthenticated == true ? Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!)
            : null;

        protected Guid? CurrentSessionIdOrNull =>
            Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sid), out var sessionId) ? sessionId : null;

        protected string CurrentUserRole =>
            User.FindFirstValue(ClaimTypes.Role)!;
    }
}