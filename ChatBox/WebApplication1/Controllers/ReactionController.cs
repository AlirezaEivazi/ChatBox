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
    public class ReactionsController : ControllerBase
    {
        private readonly ChatAppDbContext _context;

        public ReactionsController(ChatAppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> AddReaction([FromBody] ReactionDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var reaction = new MessageReaction
            {
                Reaction = dto.Reaction,
                ReactedBy = username,
                ReactedAt = DateTime.UtcNow,
                MessageId = dto.MessageId,
                PrivateMessageId = dto.PrivateMessageId
            };

            _context.MessageReactions.Add(reaction);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                reaction.Id,
                reaction.Reaction,
                reaction.ReactedBy,
                reaction.ReactedAt,
                reaction.MessageId,
                reaction.PrivateMessageId
            });
        }

        [HttpDelete("{reactionId}")]
        public async Task<IActionResult> RemoveReaction(int reactionId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var reaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.Id == reactionId && r.ReactedBy == username);

            if (reaction == null)
                return NotFound();

            _context.MessageReactions.Remove(reaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("message/{messageId}")]
        public async Task<IActionResult> GetMessageReactions(int messageId)
        {
            var reactions = await _context.MessageReactions
                .Where(r => r.MessageId == messageId)
                .Select(r => new
                {
                    r.Id,
                    r.Reaction,
                    r.ReactedBy,
                    r.ReactedAt
                })
                .ToListAsync();

            return Ok(reactions);
        }

        [HttpGet("private/{privateMessageId}")]
        public async Task<IActionResult> GetPrivateMessageReactions(int privateMessageId)
        {
            var reactions = await _context.MessageReactions
                .Where(r => r.PrivateMessageId == privateMessageId)
                .Select(r => new
                {
                    r.Id,
                    r.Reaction,
                    r.ReactedBy,
                    r.ReactedAt
                })
                .ToListAsync();

            return Ok(reactions);
        }
    }
}