using ChatAppBackend.Data;
using ChatAppBackend.DTOs;
using ChatAppBackend.Hubs;
using ChatAppBackend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatAppBackend.Services
{
    public class FileService
    {
        private readonly ChatAppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<ChatHub> _hubContext;

        public FileService(
            ChatAppDbContext context,
            IWebHostEnvironment environment,
            IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _environment = environment;
            _hubContext = hubContext;
        }

        public async Task<FileAttachment> UploadFile(IFormFile file, FileUploadDto dto, string username)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new FileAttachment
            {
                FileName = file.FileName,
                FilePath = $"/uploads/{uniqueFileName}",
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedBy = username
            };

            if (dto.RoomId.HasValue)
            {
                var message = new Message
                {
                    ChatRoomId = dto.RoomId.Value,
                    SenderUsername = username,
                    Text = dto.MessageText ?? "File attachment",
                    Timestamp = DateTime.UtcNow,
                    Status = MessageStatus.Sent
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                attachment.MessageId = message.Id;
            }
            else if (!string.IsNullOrEmpty(dto.ReceiverUsername))
            {
                var privateMessage = new PrivateMessage
                {
                    SenderUsername = username,
                    ReceiverUsername = dto.ReceiverUsername,
                    Text = dto.MessageText ?? "File attachment",
                    Timestamp = DateTime.UtcNow,
                    Status = MessageStatus.Sent
                };

                _context.PrivateMessages.Add(privateMessage);
                await _context.SaveChangesAsync();
                attachment.PrivateMessageId = privateMessage.Id;
            }

            _context.FileAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            if (attachment.MessageId.HasValue)
            {
                await _hubContext.Clients.Group(dto.RoomId.Value.ToString())
                    .SendAsync("ReceiveMessage", new
                    {
                        Id = attachment.MessageId,
                        Username = username,
                        Message = dto.MessageText ?? "File attachment",
                        Timestamp = DateTime.UtcNow,
                        Status = MessageStatus.Sent,
                        Attachment = new
                        {
                            attachment.Id,
                            attachment.FileName,
                            attachment.FilePath,
                            attachment.ContentType,
                            attachment.FileSize
                        }
                    });
            }
            else if (attachment.PrivateMessageId.HasValue)
            {
                var receiverConnections = ChatHub.OnlineUsers
                    .Where(x => x.Value == dto.ReceiverUsername)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var connectionId in receiverConnections)
                {
                    await _hubContext.Clients.Client(connectionId)
                        .SendAsync("ReceivePrivateMessage", new
                        {
                            Id = attachment.PrivateMessageId,
                            SenderUsername = username,
                            Message = dto.MessageText ?? "File attachment",
                            Timestamp = DateTime.UtcNow,
                            Status = MessageStatus.Sent,
                            Attachment = new
                            {
                                attachment.Id,
                                attachment.FileName,
                                attachment.FilePath,
                                attachment.ContentType,
                                attachment.FileSize
                            }
                        });
                }

                // Corrected caller notification
                var senderConnectionId = _hubContext.GetConnectionId(username);
                if (!string.IsNullOrEmpty(senderConnectionId))
                {
                    await _hubContext.Clients.Client(senderConnectionId)
                        .SendAsync("ReceivePrivateMessage", new
                        {
                            Id = attachment.PrivateMessageId,
                            SenderUsername = username,
                            Message = dto.MessageText ?? "File attachment",
                            Timestamp = DateTime.UtcNow,
                            Status = MessageStatus.Sent,
                            Attachment = new
                            {
                                attachment.Id,
                                attachment.FileName,
                                attachment.FilePath,
                                attachment.ContentType,
                                attachment.FileSize
                            }
                        });
                }
            }

            return attachment;
        }

        public async Task DeleteFile(int fileId, string username)
        {
            var file = await _context.FileAttachments.FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null)
                throw new KeyNotFoundException("File not found");

            if (file.UploadedBy != username)
                throw new UnauthorizedAccessException("You can only delete your own files");

            var filePath = Path.Combine(_environment.WebRootPath, file.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.FileAttachments.Remove(file);
            await _context.SaveChangesAsync();
        }
    }
}