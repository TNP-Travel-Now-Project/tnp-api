using AuthApi.Domain.Exceptions.User;

namespace AuthApi.Domain.ObjectValues
{
    public sealed record AgeUser
    {
        public int Value { get; init; }

        private AgeUser(int age) => Value = age;

        public static AgeUser Create(int age)
        {
            if (age < 18 || age > 25)
            {
                throw new UserAgeNotValid(age);
            }

            return new AgeUser(age);
        }

        public static implicit operator AgeUser(int age) => Create(age);
        public static implicit operator int(AgeUser ageUser) => ageUser.Value;
    }
}
