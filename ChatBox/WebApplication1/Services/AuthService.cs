using ChatAppBackend.Data;
using ChatAppBackend.DTOs;
using ChatAppBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChatAppBackend.Services
{
    public class AuthService
    {
        private readonly ChatAppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(ChatAppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<User> Register(UserRegisterDto dto)
        {
            CreatePasswordHash(dto.Password, out byte[] hash, out byte[] salt);
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = hash,
                PasswordSalt = salt,
                DisplayName = dto.Username,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        
        public async Task<string?> Login(UserLoginDto dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == dto.Username);
            if (user == null || !VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
                return null;

            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            using var hmac = new HMACSHA512();
            salt = hmac.Key;
            hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private bool VerifyPassword(string password, byte[] hash, byte[] salt)
        {
            using var hmac = new HMACSHA512(salt);
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computed.SequenceEqual(hash);
        }
    }
}
