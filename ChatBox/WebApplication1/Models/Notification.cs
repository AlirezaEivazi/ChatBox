namespace ChatAppBackend.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string RecipientUsername { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? RelatedEntityType { get; set; } 
        public int? RelatedEntityId { get; set; }
    }
}
