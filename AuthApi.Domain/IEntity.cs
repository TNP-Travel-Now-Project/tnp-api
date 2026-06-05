namespace AuthApi.Domain;

public interface IEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? CreatedById { get; set; }
    Guid? UpdatedById { get; set; }
}
