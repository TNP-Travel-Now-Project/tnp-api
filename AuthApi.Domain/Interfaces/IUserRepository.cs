using AuthApi.Domain.Entities.Common;

namespace AuthApi.Domain.Interfaces;

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
