using User = AuthApi.Domain.Entities.Users;
using AuthApi.Domain.Interfaces;


namespace AuthApi.Infrastructure.Persistence.Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _dbContext;

        public UserRepository(AppDbContext dbContext) => _dbContext = dbContext;

        public async Task<User> AddAsync(User user)
        {
            //await _dbContext.Users.AddAsync(user);
            return user;
        }

        public async Task<int> CommitAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            //return await _dbContext.Users.FirstOrDefaultAsync(p => p.Id == id);
            return new User();
        }
    }
}
