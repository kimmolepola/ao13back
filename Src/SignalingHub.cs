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
        bool ok = UserInfo.AddConnectedUserUnique(id, Clients);

        if (!ok)
        {
            Console.WriteLine("duplicateSessionError " + id);
            await Clients.Caller.SendAsync("duplicateSessionError");
        }
        else
        {
            await Clients.Caller.SendAsync("connect");
            await Clients.Caller.SendAsync("init", id);

            if (UserInfo.Main is null)
            {
                Console.WriteLine("main " + id);
                UserInfo.Main = id;
                await Clients.Caller.SendAsync("main", id);
            }
            else
            {
                Console.WriteLine("connectToMain " + UserInfo.Main);
                await Clients.Caller.SendAsync("connectToMain", UserInfo.Main);
            }
        }
    }

    public async Task Signaling(SignalingArgs args)
    {
        string id = Context.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
        Console.WriteLine("Signaling: " + id + " -> " + args.RemoteId);
        IHubCallerClients remoteCallerClients = UserInfo.GetConnectedUser(args.RemoteId);
        object o = new { id, description = args.Description, candidate = args.Candidate };
        await remoteCallerClients.Caller.SendAsync("signaling", o);
    }

    public static async void Disconnect(string? id)
    {
        Console.WriteLine("Disconnect: " + id);
        if (id == null) { return; }
        IHubCallerClients? Clients = UserInfo.GetConnectedUser(id);
        if (Clients == null) { return; }
        await Clients.All.SendAsync("peerDisconnected", id);
        UserInfo.RemoveConnectedUser(id);
        if (UserInfo.IsMain(id))
        {
            UserInfo.Main = null;
            await Clients.All.SendAsync("mainDisconnected", id);
            foreach ((string key, IHubCallerClients value) in UserInfo.GetConnectedUsers())
            {
                if (UserInfo.Main == null)
                {
                    UserInfo.Main = key;
                    Console.WriteLine("main: " + key);
                    await value.Caller.SendAsync("main", key);
                }
                else
                {
                    Console.WriteLine("connect to main: " + UserInfo.Main);
                    await value.Caller.SendAsync("connectToMain", UserInfo.Main);
                }
            }
        }
        await Clients.Caller.SendAsync("disconnect");
    }
}