namespace ChatAppBackend.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}
