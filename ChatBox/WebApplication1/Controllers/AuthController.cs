using ChatAppBackend.DTOs;
using ChatAppBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            if (await _authService.UserExists(dto.Username))
                return BadRequest("User already exists");

            var user = await _authService.Register(dto);
            return Ok(new { user.Id, user.Username });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            var token = await _authService.Login(dto);
            if (token == null)
                return Unauthorized();

            return Ok(new { token });
        }
    }
}
