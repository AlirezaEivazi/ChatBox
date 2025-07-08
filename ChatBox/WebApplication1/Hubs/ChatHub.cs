using ChatBox.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using ChatBox.Models;

namespace ChatBox.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatContext _context;

        public ChatHub(ChatContext context)
        {
            _context = context;
        }

        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserJoined",
                $"{Context.User.Identity.Name} joined the room");
        }

        public async Task SendMessage(string roomId, string content)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var message = new Message
            {
                Content = content,
                RoomId = roomId,
                SenderId = int.Parse(userId),
                Timestamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            await Clients.Group(roomId).SendAsync("ReceiveMessage", new
            {
                Sender = Context.User.Identity.Name,
                Content = content,
                Timestamp = message.Timestamp.ToString("g")
            });
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("UserConnected", Context.User.Identity.Name);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Clients.All.SendAsync("UserDisconnected", Context.User.Identity.Name);
            await base.OnDisconnectedAsync(exception);
        }
    }
}