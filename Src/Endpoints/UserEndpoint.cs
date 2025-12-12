using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;


namespace ao13back.Src;

class UserEndpoint
{
    public UserEndpoint(WebApplication app)
    {
        app.MapPost("/api/v1/user/savePlayerData", (PlayerState[] playerStates) =>
        {
            Console.WriteLine("savePlayerData " + playerStates.Length);
            foreach (PlayerState playerState in playerStates)
            {
                Console.WriteLine("savePlayerData " + playerState.ClientId + " " + playerState.Score);
            }
            return Results.Ok(new
            {
                success = true
            });
        }).RequireAuthorization();

        app.MapGet("/api/v1/user/checkOkToStart", (HttpContext http) =>
        {
            string? userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            ISingleClientProxy? connectedUser = UserInfo.GetConnectedUser(userId);

            if (connectedUser == null)
            {
                return Results.Ok(new
                {
                    success = true
                });
            }
            return Results.Conflict(new
            {
                success = false,
                error = "Session already open with this user"
            });

        }).RequireAuthorization();

        app.MapGet("/api/v1/user", (AppDbContext db, HttpContext http) =>
        {
            Console.WriteLine("user");
            string? userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            User? user = db.Users.SingleOrDefault(u => u.Id == userId);
            string token = http.Request.Headers.Authorization.ToString().Replace("Bearer ", "");

            return Results.Ok(new
            {
                score = user?.Score,
                userId = user?.Id,
                username = user?.UserName,
                token,
            });
        }).RequireAuthorization();

        app.MapPost("/api/v1/user/updateUsername", async (AppDbContext db, HttpContext http, UpdateUsername data) =>
        {
            Console.WriteLine("updateUsername " + data.Username);
            string? userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            User? user = db.Users.SingleOrDefault(u => u.Id == userId);
            if (user is null) return Results.NotFound();
            user.UserName = data.Username;
            await db.SaveChangesAsync();
            return Results.Ok(new { username = user.UserName });
        }).RequireAuthorization();
    }
}
