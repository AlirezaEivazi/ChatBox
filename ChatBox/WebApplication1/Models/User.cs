using ChatAppBackend.Models;

namespace ChatAppBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;
        public bool IsOnline { get; set; } = false;
        public DateTime LastSeen { get; set; }

        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();
        public ICollection<PrivateMessage> PrivateMessagesSent { get; set; } = new List<PrivateMessage>();
        public ICollection<PrivateMessage> PrivateMessagesReceived { get; set; } = new List<PrivateMessage>();
    }
}
