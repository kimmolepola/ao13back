using Microsoft.AspNetCore.SignalR;

namespace ao13back.Src;

public class ServerSignalingHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        string id = Context.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
        string isServer = Context.User.Claims.Where(c => c.Type == "isServer").Select(c => c.Value).SingleOrDefault();
        Console.WriteLine("Server OnConnected " + id + " " + isServer);
        if (isServer == "true")
        {
            ServerInfo.ConnectedServer = Clients;
            await Clients.Caller.SendAsync("connected");
        }
        else
        {
            Context.Abort();
            Console.WriteLine("Server OnConnected, not a server " + id + " " + isServer);
        }
    }

    public async Task Signaling(SignalingArgs args)
    {
        string id = Context.User.Claims.Where(c => c.Type == "name").Select(c => c.Value).SingleOrDefault();
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
}