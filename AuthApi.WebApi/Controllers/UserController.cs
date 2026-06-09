using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Users.DTOs;
using AuthApi.Application.Features.Users.Queries.GetUserById;
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
        IUserReadRepository _userRepo) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<UserListItemDto>), StatusCodes.Status200OK)]
        public async Task<List<UserListItemDto>> GetUsers()
        {
            return await _userRepo.GetAllUsersAsync();
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

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDetailResponse>> GetUserById(Guid id)
        {
            var result = await _mediator.Send(new GetUserByIdQuery(id));

            if (result.IsFailure && result.Error?.Code == ErrorCodes.UserNotFound)
                return NotFound(new ApiErrorResponse(result.Error!));

            if (result.IsFailure && result.Error?.Code == ErrorCodes.Forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse(result.Error!));

            if (result.IsFailure && result.Error?.Code == ErrorCodes.Unauthorized)
                return Unauthorized(new ApiErrorResponse(result.Error!));

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
        }

    }
}
