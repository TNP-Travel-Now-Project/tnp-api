namespace AuthApi.Application.Abstractions.Repositories.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
    }
