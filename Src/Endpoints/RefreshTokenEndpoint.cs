
namespace ao13back.Src;

class RefreshTokenEndpoint
{
    public RefreshTokenEndpoint(WebApplication app, ConfigurationManager Configuration)
    {
        app.MapGet("/api/v1/auth/refreshToken", async (AppDbContext db, HttpContext http, string refreshToken) =>
        {
            var hashed = TokenHelpers.HashToken(refreshToken);
            var tokenEntity = db.RefreshTokens
                .FirstOrDefault(t => t.TokenHash == hashed);

            if (tokenEntity == null || tokenEntity.Revoked || tokenEntity.Expires < DateTime.UtcNow)
                return Results.Unauthorized();


            // rotate: revoke old, issue new
            tokenEntity.Revoked = true;

            var newRefreshToken = TokenHelpers.CreateRefreshToken(tokenEntity.UserId, tokenEntity.Role);
            db.RefreshTokens.Add(newRefreshToken);
            await db.SaveChangesAsync();

            var newAccessToken = TokenHelpers.CreateAccessToken(tokenEntity.UserId, tokenEntity.Role, Configuration);

            return Results.Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        });
    }
}