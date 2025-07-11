namespace ChatAppBackend.Models
{
    public class MessageEditLog
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public string? OldContent { get; set; }
        public string? NewContent { get; set; }
        public DateTime EditedAt { get; set; } = DateTime.UtcNow;
        public string EditedBy { get; set; } = null!;
        public string EditReason { get; set; } = null!;
    }
}
