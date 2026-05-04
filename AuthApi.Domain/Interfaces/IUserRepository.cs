using AuthApi.Domain.Entities;

namespace AuthApi.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<Users> AddAsync(Users user);
        Task<Users?> GetUserByIdAsync(Guid id);
        Task<int> CommitAsync();
    }
    public interface ICategoryRepository
    {
        Task<Category> AddAsync(Category user);
    }
}
