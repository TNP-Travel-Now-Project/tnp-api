using AuthApi.Application.Abstractions.Interfaces.Repositories.User;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Users.Commands.AssignRoles;
using AuthApi.Application.Features.Users.Commands.RemoveRoles;
using AuthApi.Application.Features.Users.Commands.UpdateUser;
using AuthApi.Application.Features.Users.DTOs;
using AuthApi.Application.Features.Users.Queries.GetUserById;
using AuthApi.Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController(
        IMediator _mediator,
        IUserWriteRepository _userWriteRepo) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<UserListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserListItemDto>>> GetUsers()
        {
            var result = await _mediator.Send(new GetAllUserQuery());

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(new ApiErrorResponse(result.Error!));
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

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
        {
            await _mediator.Send(new UpdateUserCommand(
                id,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.DateOfBirth));

            return NoContent();
        }

        [HttpPost("{id:guid}/roles")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AssignRoles(Guid id, string[] roles)
        {
            await _mediator.Send(new AssignRolesCommand(id, roles));
            return NoContent();
        }

        [HttpDelete("{id:guid}/roles")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemoveRoles(Guid id, string[] roles)
        {
            await _mediator.Send(new RemoveRolesCommand(id, roles));
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "RequireAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LockoutUser(Guid id)
        {
            var updated = await _userWriteRepo.SoftDeleteUserAsync(id);
            if (!updated)
                return NotFound(new ApiErrorResponse(new Error(ErrorCodes.NotFound, $"User with ID {id} not found")));

            return NoContent();
        }
    }
}
