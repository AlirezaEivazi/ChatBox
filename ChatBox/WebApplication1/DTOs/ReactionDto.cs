namespace ChatAppBackend.DTOs
{
    public class ReactionDto
    {
        public string Reaction { get; set; } = null!;
        public int? MessageId { get; set; }
        public int? PrivateMessageId { get; set; }
    }
}
