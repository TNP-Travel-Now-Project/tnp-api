using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.Logout;
using AuthApi.Application.Features.Auth.Commands.RefreshToken;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.Commands.ResetPassword;
using AuthApi.Application.Features.Auth.Commands.SendOTP;
using AuthApi.Application.Features.Auth.Commands.VerifyEmail;
using AuthApi.Application.Features.Auth.DTOs.Auth.Login;
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
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LoginResponse>> Login(LoginCommand command)
        {
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        //[HttpPost("register")]
        //public async Task<ActionResult> Register(RegisterCommand command)
        //{
        //    var result = await mediator.Send(command);

        //    if (result.Value == null)
        //        return BadRequest(result);

        //    var userId = result.Value.UserId;

        //    return Created($"/api/auth/{userId}", new { userId });
        //}

        //[HttpPost("verify-email")]
        //public async Task<ActionResult> VerifyEmail([FromQuery] Guid userId, [FromQuery] string token)
        //{
        //    var result = await mediator.Send(new VerifyEmailCommand(userId, token));
        //    return result.IsSuccess ? Ok(result) : BadRequest(result);
        //}

        //[HttpPost("send-otp")]
        //public async Task<ActionResult> SendOTPByEmail(string email)
        //{
        //    var result = await mediator.Send(new SendOTPCommand(email));
        //    return result.IsSuccess ? Ok(result) : BadRequest(result);
        //}

        //[HttpPost("reset-password")]
        //public async Task<ActionResult> ResetPassword(ResetPasswordCommand command)
        //{
        //    var result = await mediator.Send(command);
        //    return result.IsSuccess ? Ok(result) : BadRequest(result);
        //}

        //[HttpPost("refresh-token")]
        //public async Task<ActionResult> RefreshToken()
        //{
        //    var result = await mediator.Send(new RefreshTokenCommand());
        //    return result.IsSuccess ? Ok(result) : BadRequest(result);
        //}

        //[HttpPost("logout")]
        //public async Task<ActionResult> Logout()
        //{
        //    var result = await mediator.Send(new LogoutCommand());
        //    return result.IsSuccess ? Ok(result) : BadRequest(result);
        //}
    }
}