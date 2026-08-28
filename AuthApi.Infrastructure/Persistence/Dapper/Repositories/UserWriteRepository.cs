using AuthApi.Application.Abstractions.Interfaces.Repositories.User;
using AuthApi.Application.Abstractions.Interfaces.UnitOfWork;
using AuthApi.Application.Features.Users.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AuthApi.Infrastructure.Persistence.Dapper.Repositories
{
    internal sealed class UserWriteRepository : IUserWriteRepository
    {
        private readonly AppDbContext _dbContext;
        private readonly IUnitOfWork _uow;
        public UserWriteRepository(AppDbContext dbContext, IUnitOfWork uow)
        {
            _dbContext = dbContext;
            _uow = uow;
        }

        public async Task<bool> UpdateUserAsync(Guid userId, UpdateUserRequest req, CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users.FindAsync(userId, cancellationToken);
            if (user is null) return false;

            if (req.FirstName is not null) user.FirstName = req.FirstName;
            if (req.LastName is not null) user.LastName = req.LastName;
            if (req.PhoneNumber is not null) user.PhoneNumber = req.PhoneNumber;
            if (req.DateOfBirth is not null) user.DateOfBirth = req.DateOfBirth.Value;
            user.UpdatedAt = DateTime.UtcNow;

            return true;
        }

        public async Task AssignRolesAsync(Guid userId, string[] rolesArr, CancellationToken cancellationToken = default)
        {
            var addRoles = await _dbContext.Roles
                .Where(p => rolesArr.Contains(p.Name))
                .Where(p => !_dbContext.UserRoles.Any(o => o.UserId == userId && o.RoleId == p.Id))
                .Select(p => new IdentityUserRole<Guid>
                {
                    UserId = userId,
                    RoleId = p.Id
                })
                .ToListAsync(cancellationToken);

            if (addRoles.Count == 0) return;
            await _dbContext.UserRoles.AddRangeAsync(addRoles);
        }

        public async Task RemoveRolesAsync(Guid userId, string[] rolesArr, CancellationToken cancellationToken = default)
        {
            var roleId = await _dbContext.Roles.Where(p => rolesArr.Contains(p.Name))
                                                .Select(p => p.Id)
                                                .ToListAsync(cancellationToken);

            var existRoles = await _dbContext.UserRoles.Where(p => p.UserId == userId && roleId.Contains(p.RoleId))
                                                        .ToListAsync(cancellationToken);
            if (!existRoles.Any()) return;

            _dbContext.UserRoles.RemoveRange(existRoles);
        }

        public async Task<bool> SoftDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var authUser = await _dbContext.Users.FindAsync(userId, cancellationToken);
            var domainUser = await _dbContext.AppUsers.FindAsync(userId, cancellationToken);
            if (authUser is null || domainUser is null) return false;

            _dbContext.AppUsers.Remove(domainUser);
            _dbContext.Users.Remove(authUser);

            return true;
        }
    }
}
