using AuthApi.Application.Abstractions.Interfaces.Repositories;
using AuthApi.Application.Features.Users.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.ComponentModel;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AuthApi.WebApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController(IUserQueryRepository _userQuery) : ControllerBase
    {
        [HttpGet]
        public async Task<List<UserDto>> GetUsers()
        {
            return await _userQuery.GetAllUserAsync();
        }

        [HttpGet("{id}")]
        public string GetUserById(int id)
        {
            return "value";
        }
    }
}
