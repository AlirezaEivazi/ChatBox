namespace ChatAppBackend.DTOs
{
    public class MessageUpdateDto
    {
        public string Text { get; set; } = null!;
        public string? Reason { get; set; }
    }
}
