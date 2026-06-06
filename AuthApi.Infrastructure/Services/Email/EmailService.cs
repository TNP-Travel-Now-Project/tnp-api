using AuthApi.Application.Abstractions.Repositories.Email;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace AuthApi.Infrastructure.Services.Email
{
    public class EmailService(IConfiguration _config) : IEmailService
    {
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtp = _config["Email:Smtp"];
            var port = Convert.ToInt32(_config["Email:Port"]);
            var from = _config["Email:From"] ?? string.Empty;
            var password = _config["Email:Password"];

            var client = new SmtpClient(smtp, port)
            {
                Credentials = new NetworkCredential(from, password),
                EnableSsl = true
            };

            var mail = new MailMessage(from, to, subject, body)
            {
                IsBodyHtml = true,
            };

            await client.SendMailAsync(mail);
        }
    }
}
