using AuthApi.Application.Features.Auth.Commands.CreateUser;
using AuthApi.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers
{
    [Route("api/auths")]
    [ApiController]
    public class AuthController(IMediator mediator) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterCommand command)
        {
            var id = await mediator.Send(command);

            return Created($"/api/auth/{id}", new { id });
        }

        [HttpGet("login")]
        public async Task<ActionResult> Login(LoginCommand command)
        {
            var result = await mediator.Send(command);

            return Ok(result);
        }
    }
}
