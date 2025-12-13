using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace ao13back.Src;


static class TokenHelpers
{
    public static string CreateAccessToken(string userId, string role, ConfigurationManager Configuration)
    {
        var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

        var signingKey = Configuration["JWTOptions:SigningKey"];
        var tokenExpirationMinutes = int.Parse(Configuration["JWTOptions:ExpirationMinutes"]);
        var issuer = Configuration["JWTOptions:Issuer"];
        var audience = Configuration["JWTOptions:Audience"];

        Console.WriteLine("--tokenExpirationMinutes: " + tokenExpirationMinutes);

        if (signingKey == null || issuer == null || audience == null || !(tokenExpirationMinutes > 0))
        {
            Console.WriteLine("Missing signingKey or issuer or audience or tokenExpirationMinutes");
            throw new Exception();
        }
        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        var symmetricKey = new SymmetricSecurityKey(keyBytes);
        var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(tokenExpirationMinutes),
            signingCredentials: signingCredentials);

        var rawToken = new JwtSecurityTokenHandler().WriteToken(token);
        return rawToken;
    }

    public static (string plainToken, RefreshToken Entity) CreateRefreshToken(string userId, string role)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var base64String = Convert.ToBase64String(randomBytes);
        var newEntity = new RefreshToken
        {
            TokenHash = HashToken(base64String),
            UserId = userId,
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            Revoked = false,
            Role = role,
        };
        return (base64String, newEntity);
        ;
    }

    public static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

}

