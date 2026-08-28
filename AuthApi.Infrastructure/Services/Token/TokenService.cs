using AuthApi.Application.Abstractions.Interfaces.Auth;
using AuthApi.Application.Abstractions.Interfaces.UnitOfWork;
using AuthApi.Application.Common;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Infrastructure.Common;
using AuthApi.Infrastructure.Identities;
using AuthApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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
        IUnitOfWork _uow,
        UserManager<ApplicationUser> _userManager,
        IOptions<AppSettings> _appSetting,
        IAuthCookieService _tokenHandler) : ITokenService
    {
        public async Task<AuthResponse> GenerateTokenServiceAsync(AuthUserDto user, IList<string> roles, int expiredDay = 30)
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
            await _uow.SaveChangesAsync();

            _tokenHandler.SetRefreshTokenCookie(token: refreshToken, days: expiredDay);
            _tokenHandler.SetCSRFTokenCookie(days: expiredDay);

            return new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: null,
                AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(expiredMinute));
        }

        private string GeneralJwtToken(AuthUserDto user, IList<string> roles, int expired)
        {
            var claims = new List<Claim>
            {
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
               new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
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

        public async Task<Result<AuthResponse>> RefreshTokenServiceAsync()
        {
            var refreshTokenToCookie = _tokenHandler.GetRefreshTokenCookie();
            if (refreshTokenToCookie == null)
                return Result<AuthResponse>.Fail(new Error(ErrorCodes.TokenRefreshError, "RefreshToken not exist!"));

            var refreshTokenEntity = await _dbContext.RefreshToken.FirstOrDefaultAsync(p => p.Token == refreshTokenToCookie
                                                                                        && p.ExpiresAt > DateTime.UtcNow
                                                                                        && !p.IsRevoked);
            if (refreshTokenEntity == null)
                return Result<AuthResponse>.Fail(new Error(ErrorCodes.TokenRefreshError, "Invalid or expired RefreshToken!"));

            var user = await _userManager.FindByIdAsync(refreshTokenEntity.UserId.ToString());

            if (user == null)
                return Result<AuthResponse>.Fail(new Error(ErrorCodes.UserNotFound, "User not found in database"));

            var roles = await _userManager.GetRolesAsync(user);

            refreshTokenEntity.IsRevoked = true;
            await _uow.SaveChangesAsync();

            return Result<AuthResponse>.Success(await GenerateTokenServiceAsync(new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName!,
                Roles = [.. roles]
            }, roles));
        }

        public async Task RevokeRefreshTokenServiceAsync()
        {
            string refreshTokenToCookie = _tokenHandler.GetRefreshTokenCookie()!;
            if (refreshTokenToCookie == null) return;

            var entity = await _dbContext.RefreshToken.FirstOrDefaultAsync(rt => rt.Token == refreshTokenToCookie);
            if (entity == null) return;

            entity.IsRevoked = true;
            await _uow.SaveChangesAsync();
        }
    }
}