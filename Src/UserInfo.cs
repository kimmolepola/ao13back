using Microsoft.AspNetCore.SignalR;

namespace ao13back.Src;
public static class UserInfo
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

    public static IHubCallerClients? GetConnectedUser(string id)
    {
        if (ConnectedUsers.TryGetValue(id, out IHubCallerClients? value))
        {
            return value;
        }
        Console.WriteLine("ConnectedUsers, not present: " + id);
        return null;
    }

    public static bool IsMain(string id)
    {
        return Main == id;
    }
}