namespace ChatAppBackend.Models
{
    public class ChatRoomMember
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public ChatRoomMemberRole Role { get; set; } = ChatRoomMemberRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; } = null!;
    }

    public enum ChatRoomMemberRole
    {
        Member,
        Admin,
        Owner
    }
}
