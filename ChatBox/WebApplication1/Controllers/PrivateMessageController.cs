using ChatAppBackend.Data;
using ChatAppBackend.DTOs;
using ChatAppBackend.Models;
using ChatAppBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrivateMessagesController : ControllerBase
    {
        private readonly ChatAppDbContext _context;

        public PrivateMessagesController(ChatAppDbContext context)
        {
            _context = context;
        }

        [HttpGet("with/{username}")]
        public async Task<IActionResult> GetMessagesWithUser(string username, [FromQuery] int limit = 50)
        {
            var currentUser = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUser))
                return Unauthorized();

            var messages = await _context.PrivateMessages
                .Where(m => (m.SenderUsername == currentUser && m.ReceiverUsername == username) ||
                           (m.SenderUsername == username && m.ReceiverUsername == currentUser))
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.SenderUsername,
                    m.ReceiverUsername,
                    m.Text,
                    m.Timestamp,
                    m.EditedAt,
                    m.Status
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendPrivateMessage([FromBody] PrivateMessageDto dto)
        {
            var senderUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(senderUsername))
                return Unauthorized();

            // In a real app, you'd use SignalR hub to send real-time messages
            // This endpoint is just for persistence
            var message = new PrivateMessage
            {
                SenderUsername = senderUsername,
                ReceiverUsername = dto.ReceiverUsername,
                Text = dto.Text,
                Timestamp = DateTime.UtcNow,
                Status = MessageStatus.Sent
            };

            _context.PrivateMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message.Id,
                message.SenderUsername,
                message.ReceiverUsername,
                message.Text,
                message.Timestamp,
                message.Status
            });
        }
    }
}