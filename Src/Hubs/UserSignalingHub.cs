using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace ao13back.Src;

public class SignalingHub : Hub
{
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        string? id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        Disconnect(id);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        string? id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine("OnConnected " + id);
        if (ServerInfo.ConnectedServer is null)
        {
            Console.WriteLine("No game server, user connecting " + id);
            await Clients.Caller.SendAsync("serverError");
        }
        else
        {
            bool ok = UserInfo.AddConnectedUserUnique(id, Clients.Caller);

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
        string? id = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine("Signaling: " + id + " -> " + args.Id + " " + args.Type);
        IHubCallerClients? ServerClients = ServerInfo.ConnectedServer;
        if (ServerClients is not null)
        {
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
        else
        {
            Console.WriteLine("Signaling failure, ServerClients is null");
        }
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
}