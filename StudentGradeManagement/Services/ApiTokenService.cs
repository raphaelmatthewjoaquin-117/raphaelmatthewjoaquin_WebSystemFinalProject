using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using StudentGradeManagement.Models;

namespace StudentGradeManagement.Services;

public sealed class ApiTokenService(
    IConfiguration configuration,
    UserManager<ApplicationUser> userManager) : IApiTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    public async Task<TokenResponse> CreateAsync(ApplicationUser user)
    {
        var key = configuration["Authentication:Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key is not configured.");
        var issuer = configuration["Authentication:Jwt:Issuer"] ?? "StudentGradeManagement";
        var audience = configuration["Authentication:Jwt:Audience"] ?? "StudentGradeManagement";
        var expiresAt = DateTime.UtcNow.Add(TokenLifetime);
        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expiresAt);
    }
}
