using ChatAppBackend.Data;
using ChatAppBackend.DTOs;
using ChatAppBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly ChatAppDbContext _context;
        private readonly MessageService _messageService;

        public MessagesController(ChatAppDbContext context, MessageService messageService)
        {
            _context = context;
            _messageService = messageService;
        }

        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetMessagesByRoom(int roomId, int limit = 50)
        {
            var messages = await _context.Messages
                .Where(m => m.ChatRoomId == roomId && !m.IsDeleted)
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.SenderUsername,
                    m.Text,
                    m.Timestamp,
                    m.EditedAt,
                    m.Status
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPut("{messageId}")]
        public async Task<IActionResult> UpdateMessage(int messageId, [FromBody] MessageUpdateDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            try
            {
                var message = await _messageService.UpdateMessage(messageId, username, dto);
                return Ok(new
                {
                    message.Id,
                    message.SenderUsername,
                    message.Text,
                    message.Timestamp,
                    message.EditedAt,
                    message.Status
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId, [FromQuery] string reason)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            try
            {
                await _messageService.DeleteMessage(messageId, username, reason);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("{messageId}/history")]
        public async Task<IActionResult> GetMessageEditHistory(int messageId)
        {
            var history = await _messageService.GetMessageEditHistory(messageId);
            return Ok(history);
        }
    }
}
