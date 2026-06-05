using AuthApi.Domain.Enums;
using AuthApi.Domain.Exceptions;

namespace AuthApi.Domain.Entities.Common;

public class Users : BaseEntity, IAggregateRoot
{
    public Users() { }
    private Users(Guid Id, UserRole role, string name) : base(Id)
    {
        Role = role;
        Name = name;
    }

    public UserRole Role { get; private set; }
    public string Name { get; private set; } = null!;

    public static Users Create(int age, UserRole role, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new DomainException("Name cannot be null or empty.");
        }

        var id = Guid.CreateVersion7();

        var user = new Users(id, role, name.Trim());

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
}
