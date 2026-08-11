using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Jetar.Core.Entities;
using Jetar.Core.Interfaces;
using Jetar.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Jetar.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly JwtOptions _opt;
    private readonly IClock _clock;

    public TokenService(IOptions<JwtOptions> opt, IClock clock)
    {
        _opt = opt.Value;
        _clock = clock;

        if (string.IsNullOrWhiteSpace(_opt.Secret) || _opt.Secret.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Secret kamida 32 belgidan iborat bo'lishi kerak. .env faylida JWT_SECRET ni belgilang.");
    }

    private SigningCredentials Credentials =>
        new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Secret)), SecurityAlgorithms.HmacSha256);

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user)
    {
        var expires = _clock.UtcNow.AddMinutes(_opt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("verified", user.IsVerified ? "1" : "0")
        };

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: _clock.UtcNow.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: Credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public string CreateRefreshToken(User user)
    {
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("typ", "refresh"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            notBefore: _clock.UtcNow.UtcDateTime,
            expires: _clock.UtcNow.AddDays(_opt.RefreshTokenDays).UtcDateTime,
            signingCredentials: Credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? ValidateRefreshToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _opt.Issuer,
                ValidateAudience = true,
                ValidAudience = _opt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Secret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            if (principal.FindFirst("typ")?.Value != "refresh") return null;

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }
}
