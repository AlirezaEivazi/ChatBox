namespace ChatAppBackend.Models
{
    public class FileAttachment
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string UploadedBy { get; set; } = null!;

        public int? MessageId { get; set; }
        public Message? Message { get; set; }

        public int? PrivateMessageId { get; set; }
        public PrivateMessage? PrivateMessage { get; set; }
    }
}
