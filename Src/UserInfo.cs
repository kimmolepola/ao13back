using Microsoft.AspNetCore.SignalR;

namespace ao13back.Src;

public static class UserInfo
{
    public static string? Main { get; set; }
    private static readonly Dictionary<string, ISingleClientProxy> ConnectedUsers = [];

    public static bool AddConnectedUserUnique(string id, ISingleClientProxy Client)
    {
        Console.WriteLine("AddConnectedUserUnique: " + id);
        if (ConnectedUsers.ContainsKey(id))
        {
            return false;
        }
        ConnectedUsers[id] = Client;
        return true;
    }

    public static void RemoveConnectedUser(string id)
    {
        ConnectedUsers.Remove(id);
    }

    public static Dictionary<string, ISingleClientProxy> GetConnectedUsers()
    {
        return ConnectedUsers;
    }

    public static ISingleClientProxy? GetConnectedUser(string id)
    {
        if (ConnectedUsers.TryGetValue(id, out ISingleClientProxy? Client))
        {
            return Client;
        }
        Console.WriteLine("ConnectedUsers, not present: " + id);
        return null;
    }

    public static bool IsMain(string id)
    {
        return Main == id;
    }
}