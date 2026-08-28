namespace AuthApi.Domain;

public abstract class BaseEntity : IEntity
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }

    protected BaseEntity() { }

    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id cannot be null or empty.", nameof(id));
        }

        Id = id;
    }
}
