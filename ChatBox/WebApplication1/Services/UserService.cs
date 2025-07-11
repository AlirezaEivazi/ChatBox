using ChatAppBackend.Data;
using ChatAppBackend.DTOs;
using ChatAppBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatAppBackend.Services
{
    public class UserService
    {
        private readonly ChatAppDbContext _context;

        public UserService(ChatAppDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfileDto> GetUserProfile(string username)
        {
            var user = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => new UserProfileDto
                {
                    Username = u.Username,
                    DisplayName = u.DisplayName,
                    AvatarUrl = u.AvatarUrl,
                    IsOnline = u.IsOnline,
                    LastSeen = u.LastSeen
                })
                .FirstOrDefaultAsync();

            if (user == null)
                throw new KeyNotFoundException("User not found");

            return user;
        }

        public async Task<UserProfileDto> UpdateUserProfile(string username, UserProfileUpdateDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            user.DisplayName = dto.DisplayName;
            user.AvatarUrl = dto.AvatarUrl;

            await _context.SaveChangesAsync();

            return new UserProfileDto
            {
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                IsOnline = user.IsOnline,
                LastSeen = user.LastSeen
            };
        }

        public async Task<List<UserProfileDto>> SearchUsers(string query)
        {
            return await _context.Users
                .Where(u => u.Username.Contains(query) || u.DisplayName.Contains(query))
                .Select(u => new UserProfileDto
                {
                    Username = u.Username,
                    DisplayName = u.DisplayName,
                    AvatarUrl = u.AvatarUrl,
                    IsOnline = u.IsOnline,
                    LastSeen = u.LastSeen
                })
                .Take(10)
                .ToListAsync();
        }
    }
}