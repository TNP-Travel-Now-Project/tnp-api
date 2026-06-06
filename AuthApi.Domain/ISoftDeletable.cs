namespace AuthApi.Domain;

public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
