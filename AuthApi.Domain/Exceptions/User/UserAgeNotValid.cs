namespace AuthApi.Domain.Exceptions.User
{
    public class UserAgeNotValid : DomainException
    {
        public UserAgeNotValid(int age) : base($"User age {age} is not valid. Must be >= 18 and <= 25") { }
    }
}
