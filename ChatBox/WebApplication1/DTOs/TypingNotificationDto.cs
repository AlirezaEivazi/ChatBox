namespace ChatAppBackend.DTOs
{
    public class TypingNotificationDto
    {
        public int? RoomId { get; set; }
        public string? ReceiverUsername { get; set; } 
        public bool IsTyping { get; set; }
    }
}
