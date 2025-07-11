using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace ChatAppBackend.Hubs
{
    public static class HubContextExtensions
    {
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();

        public static void AddConnection(this IHubContext<ChatHub> context, string username, string connectionId)
        {
            _userConnections.AddOrUpdate(username, connectionId, (_, _) => connectionId);
        }

        public static void RemoveConnection(this IHubContext<ChatHub> context, string username)
        {
            _userConnections.TryRemove(username, out _);
        }

        public static string GetConnectionId(this IHubContext<ChatHub> context, string username)
        {
            return _userConnections.TryGetValue(username, out var connectionId) ? connectionId : null;
        }
    }
}