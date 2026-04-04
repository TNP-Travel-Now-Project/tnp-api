
using AuthApi.Application.Features.Users.Commands.CreateUser;
using AuthApi.Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IResult> GetAll() => Results.Ok(await mediator.Send(new GetAllUserQuery()));

        [HttpPost]
        public async Task<IResult> Create(CreateUserCommand command)
        {
            var id = await mediator.Send(command);

            return Results.Created($"/api/users/{id}", new { id });
        }
    }
}
