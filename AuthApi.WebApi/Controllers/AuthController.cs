using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.Commands.ResetPassword;
using AuthApi.Application.Features.Auth.Commands.SendOTP;
using AuthApi.Application.Features.Auth.Commands.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers
{
    [AllowAnonymous]
    [Route("api/auth"), ApiController]
    public class AuthController(IMediator mediator) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginCommand command)
        {
            var result = await mediator.Send(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterCommand command)
        {
            var result = await mediator.Send(command);

            if (result.Value == null)
                return BadRequest(result);

            var userId = result.Value.UserId;

            return Created($"/api/auth/{userId}", new { userId });
        }

        [HttpPost("verify-email")]
        public async Task<ActionResult> VerifyEmail([FromQuery] Guid userId, [FromQuery] string token)
        {
            var result = await mediator.Send(new VerifyEmailCommand(userId, token));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("send-otp")]
        public async Task<ActionResult> SendOTPByEmail(string email)
        {
            var result = await mediator.Send(new SendOTPCommand(email));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword(ResetPasswordCommand resetPass)
        {
            var result = await mediator.Send(resetPass);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        // chua
        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken(ResetPasswordCommand resetPass)
        {
            var result = await mediator.Send(resetPass);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        // chua
        [HttpPost("logout")]
        public async Task<ActionResult> Logout(ResetPasswordCommand resetPass)
        {
            var result = await mediator.Send(resetPass);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
    