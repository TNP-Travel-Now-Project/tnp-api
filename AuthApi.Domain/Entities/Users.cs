using AuthApi.Domain.Enums;
using AuthApi.Domain.ObjectValues;
using AuthApi.Domain.Exceptions;

namespace AuthApi.Domain.Entities
{
    public class Users : BaseEntity, IAggregateRoot
    {
        public Users() { }
        private Users(Guid Id, AgeUser age, UserRole role, string name) : base(Id)
        {
            Age = age;
            Role = role;
            Name = name;
        }

        public UserRole Role { get; private set; }
        public AgeUser Age { get; private set; } = null!;
        public string Name { get; private set; } = null!;

        public static Users Create(int age, UserRole role, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DomainException("Name cannot be null or empty.");
            }

            var id = Guid.CreateVersion7();

            int agePara = AgeUser.Create(age);

            var user = new Users(id, agePara, role, name.Trim());

            return user;
        }

        public void ChangeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DomainException("Name cannot be null or empty.");
            }

            Name = name.Trim();
        }

        public void ChangeAge(int age) => Age = AgeUser.Create(age);
    }
}
