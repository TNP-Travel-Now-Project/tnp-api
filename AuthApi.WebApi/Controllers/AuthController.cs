using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.Logout;
using AuthApi.Application.Features.Auth.Commands.RefreshToken;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.Commands.ResetPassword;
using AuthApi.Application.Features.Auth.Commands.SendOTP;
using AuthApi.Application.Features.Auth.Commands.VerifyEmail;
using AuthApi.Application.Features.Auth.DTOs.Login;
using AuthApi.Application.Features.Auth.DTOs.Logout;
using AuthApi.Application.Features.Auth.DTOs.RefreshToken;
using AuthApi.Application.Features.Auth.DTOs.Register;
using AuthApi.Application.Features.Auth.DTOs.ForgetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers
{
    [AllowAnonymous]
    [Route("api/auth"), ApiController]
    public class AuthController(IMediator mediator) : ControllerBase
    {
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult Me()
        {
            return Ok(new { Authenticated = User.Identity?.IsAuthenticated ?? false });
        }

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

        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RegisterResponse>> Register(RegisterCommand command)
        {
            var result = await mediator.Send(command);

            return result.IsSuccess
                ? Created($"/api/auth/{result.Value!.UserId}", result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("verify-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> VerifyEmail([FromQuery] Guid userId, [FromQuery] string token)
        {
            var result = await mediator.Send(new VerifyEmailCommand(userId, token));
            return result.IsSuccess
                ? Ok()
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("send-otp")]
        [ProducesResponseType(typeof(OtpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<OtpResponse>> SendOTPByEmail([FromQuery] string email)
        {
            var result = await mediator.Send(new SendOTPCommand(email));
            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(NewPassResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<NewPassResponse>> ResetPassword(ResetPasswordCommand command)
        {
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RefreshTokenResponse>> RefreshToken()
        {
            var result = await mediator.Send(new RefreshTokenCommand());
            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("logout")]
        [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LogoutResponse>> Logout()
        {
            var result = await mediator.Send(new LogoutCommand());
            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }
    }
}