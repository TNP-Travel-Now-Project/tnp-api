using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Repositories.Auth;
using AuthApi.Application.Features.Auth.DTOs.Auth;
using AuthApi.Application.Features.Auth.DTOs.Auth.Token;
using AuthApi.Infrastructure.Common;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthApi.Infrastructure.Services.Token
{
    public class TokenService(
        AppDbContext _dbContext,
        UserManager<ApplicationUser> _userManager,
        IHttpContextAccessor _httpContextAccessor,
        IOptions<AppSettings> _appSetting,
        IAuthCookieService _tokenHandler) : ITokenService
    {
        public async Task<AuthResponse> GenerateTokensAsync(AuthUserDto user, IList<string> roles, int expiredDay = 30)
        {
            int expiredMinute = 15;
            var accessToken = GeneralJwtToken(user, roles, expired: expiredMinute);
            var refreshToken = GenerateRefreshTokenString();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(expiredDay),
                IsRevoked = false
            };

            _dbContext.RefreshToken.Add(refreshTokenEntity);
            await _dbContext.SaveChangesAsync();

            _tokenHandler.SetAccessToken(token: accessToken, minutes: expiredMinute);
            _tokenHandler.SetRefreshToken(token: refreshToken, days: expiredDay);
            _tokenHandler.SetCSRFToken(days: expiredDay);

            return new AuthResponse(
                AccessToken: null,
                RefreshToken: null,
                AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(expiredMinute));
        }

        private string GeneralJwtToken(AuthUserDto user, IList<string> roles, int expired)
        {
            var claims = new List<Claim>
            {
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
               new Claim(ClaimTypes.Email, user.Email ?? ""),
               new Claim(ClaimTypes.Name, user.UserName),
            };

            claims.AddRange(roles.Select(name => new Claim(ClaimTypes.Role, name)));

            var jwtKey = _appSetting.Value.JwtKey ?? throw new Exception("JWT Key missing");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
              issuer: _appSetting.Value.JwtIssuer,
              audience: _appSetting.Value.JwtAudience,
              claims: claims,
              expires: DateTime.UtcNow.AddMinutes(expired),
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

        public async Task<AuthResponse> RefreshTokenAsync()
        {
            var refreshTokenToCookie = _tokenHandler.GetRefreshToken();
            if (refreshTokenToCookie == null)
                throw new SecurityTokenException("Refresh token is missing");

            var refreshTokenEntity = await _dbContext.RefreshToken.FirstOrDefaultAsync(p => p.Token == refreshTokenToCookie
                                                                                        && p.ExpiresAt > DateTime.UtcNow
                                                                                        && !p.IsRevoked);
            if (refreshTokenEntity == null)
                throw new SecurityTokenException("Invalid or expiredDay refresh token");

            var user = await _userManager.FindByIdAsync(refreshTokenEntity.UserId.ToString());

            if (user == null)
                throw new SecurityTokenException("User not found");

            var roles = await _userManager.GetRolesAsync(user);

            refreshTokenEntity.IsRevoked = true;
            _dbContext.RefreshToken.Update(refreshTokenEntity);
            await _dbContext.SaveChangesAsync();

            return await GenerateTokensAsync(new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName!,
                Role = string.Join(", ", roles)
            }, roles);
        }

        public async Task RevokeRefreshTokenAsync()
        {
            string refreshTokenToCookie = _httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"] ?? null!;
            if (refreshTokenToCookie == null) return;

            var entity = await _dbContext.RefreshToken.FirstOrDefaultAsync(rt => rt.Token == refreshTokenToCookie);
            if (entity == null) return;

            entity.IsRevoked = true;
            await _dbContext.SaveChangesAsync();
        }
    }
}