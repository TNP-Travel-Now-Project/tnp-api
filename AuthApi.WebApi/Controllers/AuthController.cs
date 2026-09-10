using AuthApi.Application.Features.Auth.Commands.Login;
using AuthApi.Application.Features.Auth.Commands.Logout;
using AuthApi.Application.Features.Auth.Commands.RefreshToken;
using AuthApi.Application.Features.Auth.Commands.Register;
using AuthApi.Application.Features.Auth.Commands.ResetPassword;
using AuthApi.Application.Features.Auth.Commands.SendOTP;
using AuthApi.Application.Features.Auth.Commands.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Application.Features.Auth.Queries.Me;
using AuthApi.Application.Features.Auth.Commands.GoogleLogin;

namespace AuthApi.WebApi.Controllers
{
    [AllowAnonymous]
    [Route("api/auth"), ApiController]
    public class AuthController(IMediator _mediator) : ControllerBase
    {
        [Authorize, HttpGet("me")]
        [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MeResponse>> Me()
        {
            var result = await _mediator.Send(new MeQuery());

            if (result.IsFailure && result.Error?.Code == ErrorCodes.UserNotFound)
                return NotFound(new ApiErrorResponse(result.Error!));

            return result.IsSuccess
                ? Ok(result.Value)
                : Unauthorized(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LoginResponse>> Login(LoginCommand command)
        {
            var result = await _mediator.Send(command);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [AllowAnonymous, HttpPost("google-login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LoginResponse>> GoogleLogin(GoogleLoginCommand command)
        {
            var result = await _mediator.Send(command);
            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RegisterResponse>> Register(RegisterCommand command)
        {
            var result = await _mediator.Send(command);

            return result.IsSuccess
                ? Created($"/api/auth/{result.Value!.UserId}", result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("verify-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> VerifyEmail([FromQuery] Guid userId, [FromQuery] string token)
        {
            var result = await _mediator.Send(new VerifyEmailCommand(userId, token));
            return result.IsSuccess
                ? Ok()
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("send-otp")]
        [ProducesResponseType(typeof(OtpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<OtpResponse>> SendOTPByEmail([FromQuery] string email)
        {
            var result = await _mediator.Send(new SendOTPCommand(email));
            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(NewPassResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<NewPassResponse>> ResetPassword(ResetPassCommand command)
        {
            var result = await _mediator.Send(command);
            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("refresh-token")]
        [EnableRateLimiting("refresh")]
        [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RefreshTokenResponse>> RefreshToken()
        {
            var result = await _mediator.Send(new RefreshTokenCommand());
            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

        [HttpPost("logout")]
        [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LogoutResponse>> Logout()
        {
            var result = await _mediator.Send(new LogoutCommand());
            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }
    }
}