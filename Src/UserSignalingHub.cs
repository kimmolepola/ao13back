using Microsoft.AspNetCore.SignalR;

namespace ao13back.Src;

public class SignalingHub : Hub
{
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        string id = Context.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
        Disconnect(id);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        string id = Context.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
        Console.WriteLine("OnConnected " + id);
        if (ServerInfo.ConnectedServer is null)
        {
            Console.WriteLine("No game server, user connecting " + id);
            await Clients.Caller.SendAsync("serverError");
        }
        else
        {
            bool ok = UserInfo.AddConnectedUserUnique(id, Clients);

            if (!ok)
            {
                Console.WriteLine("Duplicate session error " + id);
                await Clients.Caller.SendAsync("duplicateSessionError");
            }
            else
            {
                Console.WriteLine("Initialize " + id);
                await Clients.Caller.SendAsync("init", id);
            }
        }
    }

    public async Task Signaling(SignalingArgs args)
    {
        string id = Context.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
        Console.WriteLine("Signaling: " + id + " -> " + args.Id + " " + args.Type);
        IHubCallerClients ServerClients = ServerInfo.ConnectedServer;
        object o = new
        {
            id,
            type = args.Type,
            description = args.Description,
            candidate = args.Candidate,
            mid = args.Mid
        };
        await ServerClients.Caller.SendAsync("signaling", o);
    }

    public static async void Disconnect(string? id)
    {
        Console.WriteLine("Disconnect: " + id);
        if (id == null) { return; }
        IHubCallerClients? ServerClients = ServerInfo.ConnectedServer;
        if (ServerClients == null) { return; }
        await ServerClients.All.SendAsync("peerDisconnected", id);
        UserInfo.RemoveConnectedUser(id);
    }

    // public static async void Disconnect(string? id)
    // {
    //     Console.WriteLine("Disconnect: " + id);
    //     if (id == null) { return; }
    //     IHubCallerClients? Clients = UserInfo.GetConnectedUser(id);
    //     if (Clients == null) { return; }
    //     await Clients.All.SendAsync("peerDisconnected", id);
    //     UserInfo.RemoveConnectedUser(id);
    //     if (UserInfo.IsMain(id))
    //     {
    //         UserInfo.Main = null;
    //         await Clients.All.SendAsync("mainDisconnected", id);
    //         foreach ((string key, IHubCallerClients value) in UserInfo.GetConnectedUsers())
    //         {
    //             if (UserInfo.Main == null)
    //             {
    //                 UserInfo.Main = key;
    //                 Console.WriteLine("main: " + key);
    //                 await value.Caller.SendAsync("main", key);
    //             }
    //             else
    //             {
    //                 Console.WriteLine("connect to main: " + UserInfo.Main);
    //                 await value.Caller.SendAsync("connectToMain", UserInfo.Main);
    //             }
    //         }
    //     }
    //     await Clients.Caller.SendAsync("disconnect");
    // }
}