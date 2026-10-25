using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FMS.Communication.Features.Auth.Dtos;
using FMS.Domain.entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FMS.Web.Api.Bussiness.Rules.Features.Auth.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user, Guid sessionId, IEnumerable<UserPermissionDto> menus);

    /// <summary>Gera um refresh token aleatório e o hash que deve ser persistido.</summary>
    (string Token, string Hash) CreateRefreshToken();

    string HashRefreshToken(string token);
}

public class JwtTokenService(JwtOptions options) : IJwtTokenService
{
    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user, Guid sessionId, IEnumerable<UserPermissionDto> menus)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("sid", sessionId.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("is_admin", user.IsAdmin ? "true" : "false"),
                new Claim(MenuClaims.ClaimType, MenuClaims.Serialize(menus))
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret)),
                SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    public (string Token, string Hash) CreateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (token, HashRefreshToken(token));
    }

    public string HashRefreshToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
