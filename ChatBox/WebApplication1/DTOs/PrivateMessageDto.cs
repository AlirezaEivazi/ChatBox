namespace ChatAppBackend.DTOs
{
    public class PrivateMessageDto
    {
        public string ReceiverUsername { get; set; } = null!;
        public string Text { get; set; } = null!;
    }
}
