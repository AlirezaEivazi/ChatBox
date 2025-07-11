namespace ChatAppBackend.DTOs
{
    public class UserProfileUpdateDto
    {
        public string DisplayName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }
}
