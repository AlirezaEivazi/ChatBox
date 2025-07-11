using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using ChatAppBackend.Models;
using ChatAppBackend.Data;
using Microsoft.EntityFrameworkCore;
using ChatAppBackend.DTOs;
using System.Collections.Concurrent;

namespace ChatAppBackend.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public static readonly ConcurrentDictionary<string, string> OnlineUsers = new();
        private static readonly ConcurrentDictionary<string, string> TypingUsers = new();
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();

        private readonly ChatAppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatHub(ChatAppDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                // Track connection
                UserConnections[Context.ConnectionId] = username;
                OnlineUsers[Context.ConnectionId] = username;

                // Update user status in database
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    user.IsOnline = true;
                    user.LastSeen = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                await Clients.All.SendAsync("UserOnline", username);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (OnlineUsers.TryRemove(Context.ConnectionId, out var username))
            {
                UserConnections.TryRemove(Context.ConnectionId, out _);

                // Update user status in database
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                await Clients.All.SendAsync("UserOffline", username);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(int roomId, string message)
        {
            var username = Context.User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return;

            var msg = new Message
            {
                ChatRoomId = roomId,
                SenderUsername = username,
                Text = message,
                Timestamp = DateTime.UtcNow,
                Status = MessageStatus.Sent
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", new
            {
                Id = msg.Id,
                Username = username,
                Message = message,
                Timestamp = msg.Timestamp,
                Status = msg.Status
            });
        }

        public async Task SendPrivateMessage(string receiverUsername, string message)
        {
            var senderUsername = Context.User.Identity?.Name;
            if (string.IsNullOrEmpty(senderUsername)) return;

            var privateMsg = new PrivateMessage
            {
                SenderUsername = senderUsername,
                ReceiverUsername = receiverUsername,
                Text = message,
                Timestamp = DateTime.UtcNow,
                Status = MessageStatus.Sent
            };

            _context.PrivateMessages.Add(privateMsg);
            await _context.SaveChangesAsync();

            // Notify receiver
            var receiverConnections = OnlineUsers
                .Where(x => x.Value == receiverUsername)
                .Select(x => x.Key)
                .ToList();

            foreach (var connectionId in receiverConnections)
            {
                await Clients.Client(connectionId).SendAsync("ReceivePrivateMessage", new
                {
                    Id = privateMsg.Id,
                    SenderUsername = senderUsername,
                    Message = message,
                    Timestamp = privateMsg.Timestamp,
                    Status = privateMsg.Status
                });
            }

            // Notify sender
            await Clients.Caller.SendAsync("ReceivePrivateMessage", new
            {
                Id = privateMsg.Id,
                SenderUsername = senderUsername,
                Message = message,
                Timestamp = privateMsg.Timestamp,
                Status = privateMsg.Status
            });
        }

        public async Task NotifyTyping(TypingNotificationDto dto)
        {
            var username = Context.User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return;

            if (dto.RoomId.HasValue)
            {
                await Clients.GroupExcept(dto.RoomId.Value.ToString(), Context.ConnectionId)
                    .SendAsync("UserTyping", new
                    {
                        RoomId = dto.RoomId.Value,
                        Username = username,
                        IsTyping = dto.IsTyping
                    });
            }
            else if (!string.IsNullOrEmpty(dto.ReceiverUsername))
            {
                var receiverConnections = OnlineUsers
                    .Where(x => x.Value == dto.ReceiverUsername)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var connectionId in receiverConnections)
                {
                    await Clients.Client(connectionId).SendAsync("UserTypingPrivate", new
                    {
                        SenderUsername = username,
                        IsTyping = dto.IsTyping
                    });
                }
            }
        }

        public async Task MarkMessagesAsSeen(int roomId)
        {
            var username = Context.User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return;

            var messages = await _context.Messages
                .Where(m => m.ChatRoomId == roomId &&
                           m.SenderUsername != username &&
                           m.Status != MessageStatus.Seen)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.Status = MessageStatus.Seen;
            }

            await _context.SaveChangesAsync();

            await Clients.Group(roomId.ToString()).SendAsync("MessagesSeen", new
            {
                RoomId = roomId,
                Username = username,
                MessageIds = messages.Select(m => m.Id).ToList()
            });
        }

        public async Task JoinRoom(string roomName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task LeaveRoom(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task<List<string>> GetOnlineUsers()
        {
            return OnlineUsers.Values.Distinct().ToList();
        }

        public async Task AddReaction(ReactionDto dto)
        {
            var username = Context.User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return;

            var reaction = new MessageReaction
            {
                Reaction = dto.Reaction,
                ReactedBy = username,
                ReactedAt = DateTime.UtcNow
            };

            if (dto.MessageId.HasValue)
            {
                reaction.MessageId = dto.MessageId.Value;

                var message = await _context.Messages
                    .Include(m => m.ChatRoom)
                    .FirstOrDefaultAsync(m => m.Id == dto.MessageId.Value);

                if (message != null)
                {
                    await Clients.Group(message.ChatRoomId.ToString())
                        .SendAsync("MessageReaction", new
                        {
                            MessageId = dto.MessageId.Value,
                            Reaction = dto.Reaction,
                            Username = username
                        });
                }
            }
            else if (dto.PrivateMessageId.HasValue)
            {
                reaction.PrivateMessageId = dto.PrivateMessageId.Value;

                var privateMessage = await _context.PrivateMessages
                    .FirstOrDefaultAsync(m => m.Id == dto.PrivateMessageId.Value);

                if (privateMessage != null)
                {
                    var participants = new List<string> { privateMessage.SenderUsername, privateMessage.ReceiverUsername };
                    var connections = OnlineUsers
                        .Where(x => participants.Contains(x.Value))
                        .Select(x => x.Key)
                        .ToList();

                    foreach (var connectionId in connections)
                    {
                        await Clients.Client(connectionId)
                            .SendAsync("PrivateMessageReaction", new
                            {
                                MessageId = dto.PrivateMessageId.Value,
                                Reaction = dto.Reaction,
                                Username = username
                            });
                    }
                }
            }

            _context.MessageReactions.Add(reaction);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveReaction(int reactionId)
        {
            var username = Context.User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return;

            var reaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.Id == reactionId && r.ReactedBy == username);

            if (reaction != null)
            {
                if (reaction.MessageId.HasValue)
                {
                    var messageId = reaction.MessageId.Value;
                    _context.MessageReactions.Remove(reaction);
                    await _context.SaveChangesAsync();

                    var message = await _context.Messages
                        .Include(m => m.ChatRoom)
                        .FirstOrDefaultAsync(m => m.Id == messageId);

                    if (message != null)
                    {
                        await Clients.Group(message.ChatRoomId.ToString())
                            .SendAsync("RemoveMessageReaction", new
                            {
                                MessageId = messageId,
                                ReactionId = reactionId,
                                Username = username
                            });
                    }
                }
                else if (reaction.PrivateMessageId.HasValue)
                {
                    var privateMessageId = reaction.PrivateMessageId.Value;
                    _context.MessageReactions.Remove(reaction);
                    await _context.SaveChangesAsync();

                    var privateMessage = await _context.PrivateMessages
                        .FirstOrDefaultAsync(m => m.Id == privateMessageId);

                    if (privateMessage != null)
                    {
                        var participants = new List<string> { privateMessage.SenderUsername, privateMessage.ReceiverUsername };
                        var connections = OnlineUsers
                            .Where(x => participants.Contains(x.Value))
                            .Select(x => x.Key)
                            .ToList();

                        foreach (var connectionId in connections)
                        {
                            await Clients.Client(connectionId)
                                .SendAsync("RemovePrivateMessageReaction", new
                                {
                                    MessageId = privateMessageId,
                                    ReactionId = reactionId,
                                    Username = username
                                });
                        }
                    }
                }
            }
        }

        public static string GetConnectionIdForUser(string username)
        {
            return UserConnections.FirstOrDefault(x => x.Value == username).Key;
        }
    }
}