using ChatAppBackend.Data;
using ChatAppBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatRoomsController : ControllerBase
    {
        private readonly ChatAppDbContext _context;

        public ChatRoomsController(ChatAppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<ChatRoom>>> GetChatRooms()
        {
            return await _context.ChatRooms.ToListAsync();
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ChatRoom>> CreateChatRoom([FromBody] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Room name cannot be empty.");

            var existing = await _context.ChatRooms.FirstOrDefaultAsync(r => r.Name == name);
            if (existing != null)
                return Conflict("Room name already exists.");

            var room = new ChatRoom { Name = name };
            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();

            return Ok(room);
        }

        [HttpGet("{roomId}/messages")]
        [Authorize]
        public async Task<IActionResult> GetMessages(int roomId)
        {
            var messages = await _context.Messages
                .Where(m => m.ChatRoomId == roomId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.SenderUsername,
                    m.Text,
                    m.Timestamp
                })
                .ToListAsync();

            return Ok(messages);
        }

    }
}
