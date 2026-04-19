using DnsClient;
using AuthApi.Application.Abstractions.Interfaces.Email;

namespace AuthApi.Infrastructure.Services.Email
{
    public class EmailChecker : IEmailChecker
    {
        public async Task<bool> IsValidAsync(string email)
        {
            var domain = email.Split('@').Last();

            var lookup = new LookupClient();
            var result = await lookup.QueryAsync(domain, QueryType.MX);

            return result.Answers.MxRecords().Any();
        }
    }
}
