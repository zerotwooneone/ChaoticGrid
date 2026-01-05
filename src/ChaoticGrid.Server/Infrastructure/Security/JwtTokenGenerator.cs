using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChaoticGrid.Server.Domain.Aggregates.IdentityAggregate;
using ChaoticGrid.Server.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ChaoticGrid.Server.Infrastructure.Security;

public sealed class JwtTokenGenerator(IConfiguration configuration)
{
    public string Generate(User user)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? "ChaoticGrid";
        var audience = jwtSection["Audience"] ?? "ChaoticGrid";
        var signingKey = jwtSection["SigningKey"] ?? "dev-only-signing-key-change-me some long key blah blah blah";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var permissions = user.GetEffectivePermissions();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("nickname", user.Nickname),
            new("x-permissions", ((long)permissions).ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
