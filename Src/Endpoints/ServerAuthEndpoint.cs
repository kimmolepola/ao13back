using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ao13back.Src;

class ServerAuthEndpoint
{
    public ServerAuthEndpoint(
        WebApplication app,
        ConfigurationManager Configuration)
    {
        app.MapPost("/api/v1/auth/serverLogin", async (AppDbContext db, ServerLogin data) =>
        {
            Console.WriteLine("Server login: " + data);

            bool validCredentials = data.Password != null && data.Password == Configuration["ServerOptions:LoginPassword"];

            if (!validCredentials)
            {
                return Results.Unauthorized();
            }

            try
            {
                var newRefreshToken = TokenHelpers.CreateRefreshToken(data.Id, "server");
                db.RefreshTokens.Add(newRefreshToken);
                await db.SaveChangesAsync();
                string accessToken = TokenHelpers.CreateAccessToken(data.Id.ToString(), "server", Configuration);
                return Results.Ok(new
                {
                    token = accessToken,
                });
            }
            catch (Exception)
            {
                return Results.InternalServerError();
            }
        });
    }
}