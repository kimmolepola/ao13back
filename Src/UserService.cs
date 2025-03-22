using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;


namespace ao13back.Src;

class UserService
{
    public UserService(WebApplication app)
    {
        app.MapGet("/api/v1/user/checkOkToStart", (HttpContext http) =>
        {
            string? userId = http.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
            IHubCallerClients? connectedUser = UserInfo.GetConnectedUser(userId);

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

        app.MapGet("/api/v1/user", (UserDb db, HttpContext http) =>
        {
            Console.WriteLine("user");
            string? userId = http.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
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

        app.MapPost("/api/v1/user/updateUsername", async (UserDb db, HttpContext http, UpdateUsername data) =>
        {
            Console.WriteLine("updateUsername " + data.Username);
            string? userId = http.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
            User? user = db.Users.SingleOrDefault(u => u.Id == userId);
            if (user is null) return Results.NotFound();
            user.UserName = data.Username;
            await db.SaveChangesAsync();
            return Results.Ok(new { username = user.UserName });
        }).RequireAuthorization();
    }
}
