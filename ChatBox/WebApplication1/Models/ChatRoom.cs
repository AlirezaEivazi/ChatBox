namespace ChatAppBackend.Models
{
    public class ChatRoom
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = null!;

        public List<Message> Messages { get; set; } = new();
        public List<ChatRoomMember> Members { get; set; } = new();
    }
}

