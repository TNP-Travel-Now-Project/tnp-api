namespace AuthApi.Application.Abstractions.Interfaces.Email
{
    public interface IEmailChecker
    {
        Task<bool> IsValidAsync(string email);
    }
}
