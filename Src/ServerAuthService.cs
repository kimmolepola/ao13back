using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ao13back.Src;

class ServerAuthService
{
    public ServerAuthService(
        WebApplication app,
        ConfigurationManager Configuration)
    {
        app.MapPost("/api/v1/auth/serverLogin", (ServerLogin data) =>
       {
           Console.WriteLine("Server login: " + data);

           bool validCredentials = data.Password != null && data.Password == Configuration["ServerOptions:LoginPassword"];

           if (!validCredentials)
           {
               return Results.Unauthorized();
           }

           var claims = new List<Claim>()
           {
                new ("name", "" + data.Id),
                new ("isServer", "true"),
           };

           if (Configuration["JWTOptions:SigningKey"] == null)
           {
               return Results.InternalServerError();
           }
           var keyBytes = Encoding.UTF8.GetBytes(Configuration["JWTOptions:SigningKey"]);
           var symmetricKey = new SymmetricSecurityKey(keyBytes);
           var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

           var token = new JwtSecurityToken(
               issuer: Configuration["JWTOptions:Issuer"],
               audience: Configuration["JWTOptions:Audience"],
               claims: claims,
               expires: DateTime.Now.AddSeconds(int.Parse(Configuration["JWTOptions:ExpirationSeconds"])),
               signingCredentials: signingCredentials);

           var rawToken = new JwtSecurityTokenHandler().WriteToken(token);

           return Results.Ok(new
           {
               token = rawToken,
           });
       });
    }
}