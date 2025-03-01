// using Microsoft.EntityFrameworkCore;

namespace ao13back.Src;

class Users
{
    public Users(WebApplication app)
    {
        // app.MapGet("/users", async (UserDb db) =>
        //    {
        //        return await db.Users.ToListAsync();
        //    });

        // app.MapGet("/users/{id}", async (int id, UserDb db) =>
        //     await db.Users.FindAsync(id)
        //         is User user
        //             ? Results.Ok(user)
        //             : Results.NotFound());

        // app.MapPost("/users", async (User user, UserDb db) =>
        // {
        //     db.Users.Add(user);
        //     await db.SaveChangesAsync();

        //     return Results.Created($"/todoitems/{user.Id}", user);
        // });

        // app.MapPut("/users/{id}", async (int id, User inputUser, UserDb db) =>
        // {
        //     var user = await db.Users.FindAsync(id);

        //     if (user is null) return Results.NotFound();

        //     user.UserName = inputUser.UserName;

        //     await db.SaveChangesAsync();

        //     return Results.NoContent();
        // });

        // app.MapDelete("/users/{id}", async (int id, UserDb db) =>
        // {
        //     if (await db.Users.FindAsync(id) is User user)
        //     {
        //         db.Users.Remove(user);
        //         await db.SaveChangesAsync();
        //         return Results.NoContent();
        //     }

        //     return Results.NotFound();
        // });
    }
}
