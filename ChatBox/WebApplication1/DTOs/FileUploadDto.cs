using ChatAppBackend.Controllers;

namespace ChatAppBackend.DTOs
{
    public class FileUploadDto
    {
        public IFormFile File { get; set; }
        public int? RoomId { get; set; }
        public string? ReceiverUsername { get; set; }
        public string? MessageText { get; set; }
    }
}