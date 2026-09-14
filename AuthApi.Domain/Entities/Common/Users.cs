using AuthApi.Domain.Enums;
using AuthApi.Domain.Exceptions;

namespace AuthApi.Domain.Entities.Common;

public class Users : BaseEntity, IAggregateRoot, ISoftDeletable
{
    public Users() { }
    private Users(Guid id, UserRole role, string name) : base(id)
    {
        Role = role;
        Name = name;
    }

    public UserRole Role { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime? DeletedAt { get; set; }

    public static Users Create(Guid id, UserRole role, string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new DomainException("Name cannot be null or empty.");

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
