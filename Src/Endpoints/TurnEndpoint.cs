using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace ao13back.Src;

class TurnEndpoint
{
    public TurnEndpoint(WebApplication app, ConfigurationManager Configuration)
    {
        app.MapGet("/api/v1/auth/getTurnCredentials", (HttpContext http) =>
        {
            Console.WriteLine("getTurnCredentials");
            string? userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            string HmacSecret = Configuration["TurnOptions:HmacSecret"] ?? throw new InvalidOperationException("HmacSecret is not configured.");
            if (HmacSecret == null)
            {
                return Results.InternalServerError();
            }
            long validForSeconds = 60;
            long unixTimeStamp = validForSeconds + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string username = unixTimeStamp + ":" + userId;
            HMACSHA1 hmac = new(Encoding.UTF8.GetBytes(HmacSecret));

            byte[] dataBytes = Encoding.UTF8.GetBytes(username);
            byte[] calcHash = hmac.ComputeHash(dataBytes);

            string password = Convert.ToBase64String(calcHash);
            string hostname = "" + Configuration["TurnOptions:TurnHostname"];
            int port = int.Parse("" + Configuration["TurnOptions:TurnPort"]);
            return Results.Ok(new
            {
                hostname,
                port,
                username,
                password,
            });
        }).RequireAuthorization();

    }
}
