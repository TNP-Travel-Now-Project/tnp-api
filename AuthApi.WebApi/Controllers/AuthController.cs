using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.Commands.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers
{
    [Route("api/auth"), ApiController]
    public class AuthController(IMediator mediator) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterCommand command)
        {
            var result = await mediator.Send(command);

            if (result.Value == null)
                return BadRequest(result);

            var userId = result.Value.UserId;

            return Created($"/api/auth/{userId}", new { userId });
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginCommand command)
        {
            var result = await mediator.Send(command);

            return Ok(result);
        }

        [HttpGet("verify-email")]
        public async Task<ActionResult> VerifyEmail([FromQuery] Guid userId, [FromQuery] string token)
        {
            var result = await mediator.Send(new VerifyEmailCommand(userId, token));

            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
