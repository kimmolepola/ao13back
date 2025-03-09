namespace ao13back.Src;

class GameObjectService
{
    public GameObjectService(WebApplication app)
    {
        app.MapPost("/api/v1/gameObject/saveGameState", () =>
        {
            return Results.Ok(new
            {
                success = true
            });
        }).RequireAuthorization();

        app.MapGet("/api/v1/gameObject/{id}", (string id, UserDb db) =>
        {
            Console.WriteLine("gameObject/{id} " + id);
            User? user = db.Users.SingleOrDefault(u => u.Id == id);

            return Results.Ok(new
            {
                score = user?.Score,
                username = user?.UserName,
                isPlayer = true,
            });
        }).RequireAuthorization();
    }
}
