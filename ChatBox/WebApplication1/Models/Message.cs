namespace ChatAppBackend.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string SenderUsername { get; set; } = null!;
        public string Text { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; }
        public string? DeleteReason { get; set; }
        public MessageStatus Status { get; set; } = MessageStatus.Sent;

        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; } = null!;
    }

    public enum MessageStatus
    {
        Sent,
        Delivered,
        Seen
    }
}