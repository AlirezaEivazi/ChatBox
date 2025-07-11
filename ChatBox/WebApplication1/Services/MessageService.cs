using ChatAppBackend.Data;
using ChatAppBackend.DTOs;
using ChatAppBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatAppBackend.Services
{
    public class MessageService
    {
        private readonly ChatAppDbContext _context;

        public MessageService(ChatAppDbContext context)
        {
            _context = context;
        }

        public async Task<Message> UpdateMessage(int messageId, string username, MessageUpdateDto dto)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null)
                throw new KeyNotFoundException("Message not found");

            if (message.SenderUsername != username)
                throw new UnauthorizedAccessException("You can only edit your own messages");

            var editLog = new MessageEditLog
            {
                MessageId = message.Id,
                OldContent = message.Text,
                NewContent = dto.Text,
                EditedBy = username,
                EditReason = dto.Reason ?? "No reason provided"
            };

            message.Text = dto.Text;
            message.EditedAt = DateTime.UtcNow;
            message.Status = MessageStatus.Sent; // Reset status after edit

            _context.MessageEditLogs.Add(editLog);
            await _context.SaveChangesAsync();

            return message;
        }

        public async Task DeleteMessage(int messageId, string username, string reason)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null)
                throw new KeyNotFoundException("Message not found");

            if (message.SenderUsername != username)
                throw new UnauthorizedAccessException("You can only delete your own messages");

            message.IsDeleted = true;
            message.DeleteReason = reason;
            await _context.SaveChangesAsync();
        }

        public async Task MarkMessagesAsSeen(int roomId, string username)
        {
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
        }

        public async Task<List<MessageEditLog>> GetMessageEditHistory(int messageId)
        {
            return await _context.MessageEditLogs
                .Where(log => log.MessageId == messageId)
                .OrderByDescending(log => log.EditedAt)
                .ToListAsync();
        }
    }
}