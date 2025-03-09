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
    public async Task AsdfMessage(string asdf, string qwer)
    {
        Console.WriteLine("SignalingHub: " + asdf + " - " + qwer);
        await Clients.All.SendAsync("messageReceived", asdf, qwer);
    }

    public async Task Signaling(SignalingArgs args)
    {
        Console.WriteLine("Signaling: " + args.RemoteId + " - " + args.Description + " - " + args.Candidate);
        if (args.RemoteId == null)
        {
            Console.WriteLine("RemoteId null");
            return;
        }
        IHubCallerClients remoteCallerClients = UserInfo.GetConnectedUser(args.RemoteId);
        object o = new { remoteId = args.RemoteId };
        Console.WriteLine("oooooooooooooooooooo: " + o);
        await remoteCallerClients.Caller.SendAsync("signaling", args);
    }

    public async Task Signalingx(string functionName)
    {
        Console.WriteLine("Signalingx: " + functionName);
        await Clients.All.SendAsync("messageReceived", "functionName", functionName);
    }

    public static async void Disconnect(string? id)
    {
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

    private static class UserInfo
    {
        public static string? Main { get; set; }
        private static readonly Dictionary<string, IHubCallerClients> ConnectedUsers = [];

        public static bool AddConnectedUserUnique(string id, IHubCallerClients Clients)
        {
            Console.WriteLine("AddConnectedUserUnique: " + id);
            if (ConnectedUsers.ContainsKey(id))
            {
                return false;
            }
            ConnectedUsers[id] = Clients;
            return true;
        }

        public static void RemoveConnectedUser(string id)
        {
            ConnectedUsers.Remove(id);
        }

        public static Dictionary<string, IHubCallerClients> GetConnectedUsers()
        {
            return ConnectedUsers;
        }

        public static IHubCallerClients GetConnectedUser(string id)
        {
            return ConnectedUsers[id];
        }

        public static bool IsMain(string id)
        {
            return Main == id;
        }
    }
}