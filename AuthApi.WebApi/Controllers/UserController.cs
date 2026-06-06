using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Users.DTOs;
using AuthApi.Application.Features.Users.Queries.Me;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController(
        IMediator _mediator,
        IUserQueryRepository _userQuery) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
        public async Task<List<UserDto>> GetUsers()
        {
            return await _userQuery.GetAllUserAsync();
        }

        [HttpGet("me")]
        [Authorize]
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
    }
}
