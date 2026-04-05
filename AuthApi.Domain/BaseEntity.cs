namespace AuthApi.Domain
{
    public abstract class BaseEntity : IEntity
    {
        public Guid Id { get; private set; }
        protected BaseEntity() { }
        protected BaseEntity(Guid id)
        {
            if(id == Guid.Empty)
            {
                throw new ArgumentException("Id cannot be null or empty.", nameof(id));
            }

            Id = id;
        }
    }
}
