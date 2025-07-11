namespace ChatAppBackend.Models
{
    public class MessageReaction
    {
        public int Id { get; set; }
        public string Reaction { get; set; } = null!; 
        public string ReactedBy { get; set; } = null!;
        public DateTime ReactedAt { get; set; } = DateTime.UtcNow;

        public int? MessageId { get; set; }
        public Message? Message { get; set; }

        public int? PrivateMessageId { get; set; }
        public PrivateMessage? PrivateMessage { get; set; }
    }
}
