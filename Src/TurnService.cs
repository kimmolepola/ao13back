using System.Text;
using System.Security.Cryptography;

namespace ao13back.Src;

class TurnService
{
    public TurnService(WebApplication app, TurnOptions? turnOptions)
    {
        app.MapPost("/api/v1/auth/getTurnCredentials", (UserDb db, HttpContext http) =>
        {
            Console.WriteLine("getTurnCredentials");
            string? userId = http.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
            string HmacSecret = turnOptions?.HmacSecret;
            if (HmacSecret == null)
            {
                return Results.InternalServerError();
            }
            long unixTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string username = unixTimeStamp + userId;
            HMACSHA1 hmac = new(Encoding.UTF8.GetBytes(HmacSecret));

            byte[] dataBytes = UTF8Encoding.UTF8.GetBytes(username);
            byte[] calcHash = hmac.ComputeHash(dataBytes);
            string credential = Convert.ToBase64String(calcHash);
            string urls = "turns:" + turnOptions?.TurnUrl;

            return Results.Ok(new
            {
                urls,
                username,
                credential
            });
        }).RequireAuthorization();

    }
}
