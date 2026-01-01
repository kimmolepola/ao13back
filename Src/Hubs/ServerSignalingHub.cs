using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ao13back.Src;

public class ServerSignalingHub : Hub
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(55);
    private static Timer? _heartbeatTimer;
    private static readonly ConcurrentDictionary<string, DateTime> LastHeartbeat = new();
    private static readonly TimeSpan ClientTimeout = TimeSpan.FromSeconds(165);

    public ServerSignalingHub()
    {
        // Start heartbeat timer once
        if (_heartbeatTimer == null)
        {
            _heartbeatTimer = new Timer(async _ =>
            {
                try
                {
                    await ServerInfo.ConnectedServer?.All.SendAsync("heartbeat", DateTime.UtcNow);
                }
                catch { /* swallow errors */ }

            }, null, HeartbeatInterval, HeartbeatInterval);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        string? id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        bool? isServer = Context.User?.IsInRole("server");

        Console.WriteLine("Server OnConnected, id: " + id + " isServer: " + isServer);
        if (isServer == true)
        {
            ServerInfo.ConnectedServer = Clients;
            await Clients.Caller.SendAsync("connected");
        }
        else
        {
            Context.Abort();
            Console.WriteLine("Server OnConnected, not a server, id: " + id + " isServer: " + isServer);
        }
    }

    public async Task Signaling(SignalingArgs args)
    {
        string? id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine("Signaling: " + id + " -> " + args.Id + " " + args.Type);
        ISingleClientProxy? connectedUser = UserInfo.GetConnectedUser(args.Id);
        if (connectedUser is not null)
        {
            object o = new
            {
                id,
                type = args.Type,
                description = args.Description,
                candidate = args.Candidate,
                mid = args.Mid
            };
            await connectedUser.SendAsync("signaling", o);
        }
        else
        {
            Console.WriteLine("Signaling failure, connectedUser not found with id " + args.Id);
        }
    }
    public async Task Heartbeat()
    {
        // Optional: track last heartbeat timestamp per connection
        string? name = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"Heartbeat from server {name} at {DateTime.UtcNow:O}");
        string id = Context.ConnectionId;
        LastHeartbeat[id] = DateTime.UtcNow;
    }

    // Keep reference so GC doesn't collect the timer
    private static Timer? _clientWatchdog = new Timer(_ =>
    {
        var now = DateTime.UtcNow;
        foreach (var kv in LastHeartbeat)
        {
            if (now - kv.Value > ClientTimeout)
            {
                // Remove timed-out client 
                LastHeartbeat.TryRemove(kv.Key, out DateTime _);
                Console.WriteLine($"Server {kv.Key} timed out and was removed");
            }
        }
    }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

}