using blog.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace blog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IMediator mediator) : ApiController(mediator)
    {

    }
}
