using Microsoft.AspNetCore.SignalR;

namespace ao13back.Src;

public static class ServerInfo
{
    public static IHubCallerClients? ConnectedServer { get; set; }

}