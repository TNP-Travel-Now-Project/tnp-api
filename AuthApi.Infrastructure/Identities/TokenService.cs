using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Features.Auth.DTOs.Token;
using AuthApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthApi.Infrastructure.Identities
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly AppDbContext _dbContext;

        public TokenService(UserManager<ApplicationUser> userManager, IConfiguration config, AppDbContext dbContext)
        {
            _userManager = userManager;
            _config = config;
            _dbContext = dbContext;
        }

        public async Task<AuthResponse> GenerateTokensAsync(AuthUserDto user)
        {
            var accessToken = GeneralJwtToken(user);
            var resfreshTokenStr = GenerateRefreshTokenString();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = resfreshTokenStr,
                ExpiresAt = DateTime.UtcNow.AddDays(14),
            };

            _dbContext.RefreshToken.Add(refreshTokenEntity);
            await _dbContext.SaveChangesAsync();

            return new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: resfreshTokenStr,
                AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(30));
        }

        private string GeneralJwtToken(AuthUserDto user)
        {
            var claims = new List<Claim>
            {
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
               new Claim(ClaimTypes.Email, user.Email ?? ""),
               new Claim(ClaimTypes.Name, user.FullName),
               new Claim(ClaimTypes.Role, user.Role.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
              issuer: _config["Jwt:Issuer"],
              audience: _config["Jwt:Audience"],
              claims: claims,
              expires: DateTime.UtcNow.AddMinutes(30),
              signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var refreshTokenEntity = await _dbContext.RefreshToken.FirstOrDefaultAsync(p => p.Token == refreshToken
                                                                                        && p.ExpiresAt > DateTime.UtcNow
                                                                                        && !p.IsRevoked);
            if (refreshTokenEntity == null)
            {
                throw new SecurityTokenException("Invalid or expired refresh token");
            }

            var user = await _userManager.FindByIdAsync(refreshTokenEntity.UserId.ToString());
            if (user == null)
            {
                throw new SecurityTokenException("User not found");
            }

            refreshTokenEntity.IsRevoked = true;
            _dbContext.RefreshToken.Update(refreshTokenEntity);

            return await GenerateTokensAsync(new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                
            });
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var entity = await _dbContext.RefreshToken.FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (entity != null)
            {
                entity.IsRevoked = true;
                await _dbContext.SaveChangesAsync();
            }
        }  
    }
}
