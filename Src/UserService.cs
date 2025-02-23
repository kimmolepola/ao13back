using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Net.Mail;
using System.Net;

namespace ao13back.Src;

class UserService
{
    public UserService(WebApplication app)
    {
        app.MapGet("/api/v1/user", (UserDb db, HttpContext http) =>
           {
               Console.WriteLine("User");
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

        app.MapPost("/api/v1/user/updateUsername", () =>
        {
            return Results.Ok();
        });
    }
}
