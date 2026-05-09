using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using opn_chat.Application.DTOs;
using opn_chat.Application.Interfaces;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;

namespace opn_chat.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;

        public AuthController(IAuthService authService, IUserRepository userRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
        }

        private const int Iterations = 100000;
        private const int KeySize = 32;
        private const int SaltSize = 16;

        private static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = deriveBytes.GetBytes(KeySize);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split(':');
                if (parts.Length != 2) return false;
                var salt = Convert.FromBase64String(parts[0]);
                var expectedKey = Convert.FromBase64String(parts[1]);
                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                var actualKey = deriveBytes.GetBytes(KeySize);
                return CryptographicOperations.FixedTimeEquals(expectedKey, actualKey);
            }
            catch
            {
                return false;
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "Email already registered" });

            if (await _userRepository.NicknameExistsAsync(dto.Nickname))
                return BadRequest(new { message = "Nickname already taken" });

            var user = new User
            {
                Email = dto.Email,
                Nickname = dto.Nickname,
                PasswordHash = HashPassword(dto.Password),
                GoogleId = $"local_{Guid.NewGuid()}"
            };
            await _userRepository.AddAsync(user);

            var result = await _authService.GenerateTokensAsync(user);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !VerifyPassword(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password" });

            var result = await _authService.GenerateTokensAsync(user);
            return Ok(result);
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthDto dto)
        {
            // Temporary logging
            Console.WriteLine($"Received googleToken: {dto.GoogleToken?.Substring(0, Math.Min(50, dto.GoogleToken?.Length ?? 0))}...");
            
            var result = await _authService.AuthenticateWithGoogleAsync(dto.GoogleToken);
            
            if (result == null)
                return Unauthorized(new { message = "Invalid Google token" });

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            
            if (result == null)
                return Unauthorized(new { message = "Invalid or expired refresh token" });

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
        {
            await _authService.RevokeRefreshTokenAsync(dto.RefreshToken);
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
