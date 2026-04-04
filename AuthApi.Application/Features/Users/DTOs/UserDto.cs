namespace AuthApi.Application.Features.Users.DTOs
{
    public sealed record UserDto(
        Guid Id, 
        int Age, 
        string Role, 
        string Name);
}
