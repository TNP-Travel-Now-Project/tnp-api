using System.Net.Mail;

namespace AuthApi.Domain.ObjectValues
{
    public class Email
    {
        public string Value { get; } = null!;

        private Email(string value)
        {
            Value = value;
        }

        public static Email Create(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email cannot be null or empty.");

            try
            {
                var mailAddress = new MailAddress(email);

                var parts = mailAddress.Host.Split('.');
                if (parts.Length < 2 || parts.Last().Length < 2)
                    throw new ArgumentException("Invalid Email");

                return new Email(mailAddress.Address);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Invalid email format.");
            }
        }
    }
}
