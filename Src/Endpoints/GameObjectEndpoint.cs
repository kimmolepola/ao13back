namespace ao13back.Src;

class GameObjectEndpoint
{
    public GameObjectEndpoint(WebApplication app)
    {
        app.MapPost("/api/v1/gameObject/saveGameState", (PlayerState[] playerStates) =>
        {
            Console.WriteLine("saveGameState " + playerStates.Length);
            foreach (PlayerState playerState in playerStates)
            {
                Console.WriteLine("saveGameState " + playerState.ClientId + " " + playerState.Score);

            }
            return Results.Ok(new
            {
                success = true
            });
        }).RequireAuthorization();

        app.MapGet("/api/v1/gameObject/{id}", (string id, AppDbContext db) =>
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
