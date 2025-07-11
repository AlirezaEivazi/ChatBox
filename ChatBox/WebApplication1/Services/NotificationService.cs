using ChatAppBackend.Data;
using ChatAppBackend.DTOs;
using ChatAppBackend.Hubs;
using ChatAppBackend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatAppBackend.Services
{
    public class NotificationService
    {
        private readonly ChatAppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public NotificationService(ChatAppDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<List<NotificationDto>> GetUserNotifications(string username)
        {
            return await _context.Notifications
                .Where(n => n.RecipientUsername == username)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Content = n.Content,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    RelatedEntityType = n.RelatedEntityType,
                    RelatedEntityId = n.RelatedEntityId
                })
                .ToListAsync();
        }

        public async Task MarkAsRead(int notificationId, string username)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUsername == username);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsRead(string username)
        {
            var notifications = await _context.Notifications
                .Where(n => n.RecipientUsername == username && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendNotification(string recipient, string content, string? relatedEntityType = null, int? relatedEntityId = null)
        {
            var notification = new Notification
            {
                RecipientUsername = recipient,
                Content = content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send real-time notification
            var recipientConnections = ChatHub.OnlineUsers
                .Where(x => x.Value == recipient)
                .Select(x => x.Key)
                .ToList();

            foreach (var connectionId in recipientConnections)
            {
                await _hubContext.Clients.Client(connectionId)
                    .SendAsync("ReceiveNotification", new
                    {
                        notification.Id,
                        notification.Content,
                        notification.IsRead,
                        notification.CreatedAt,
                        notification.RelatedEntityType,
                        notification.RelatedEntityId
                    });
            }
        }
    }
}