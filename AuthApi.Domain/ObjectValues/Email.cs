using System.Net.Mail;

namespace AuthApi.Domain.ObjectValues
{
    public class Email
    {
        public string Value { get; } = null!;

        private Email(string value) => Value = value;

        public static Email Create(string email)
        {
            if (!TryCreate(email, out var result))
                throw new ArgumentException("Invalid email");

            return result!;
        }

        public static bool IsValid(string email) => TryCreate(email, out _);

        public static bool TryCreate(string email, out Email? result)
        {
            result = null;

            if (string.IsNullOrEmpty(email))
                return false;

            try
            {
                var mailAddress = new MailAddress(email);

                var parts = mailAddress.Host.Split('.');
                if (parts.Length < 2 || parts.Last().Length < 2)
                    return false;

                result = new Email(mailAddress.Address);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}