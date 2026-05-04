namespace AuthApi.Domain.Entities
{
    public class Category : BaseEntity
    {
        public Guid? UserId { get; set; }
        public Users? User { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Expense";
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; } = null!;
        public ICollection<Category> SubCategories { get; set; } = null!;
    }
}
