namespace ChatAppBackend.DTOs
{
    public class UserProfileDto
    {
        public string Username { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
    }
} 
