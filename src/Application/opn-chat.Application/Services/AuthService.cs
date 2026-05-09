using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;
using opn_chat.Application.DTOs;
using opn_chat.Application.Interfaces;
using opn_chat.Domain.Entities;
using opn_chat.Domain.Interfaces;

namespace opn_chat.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtGenerator _jwtGenerator;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IJwtGenerator jwtGenerator,
            IConfiguration configuration,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtGenerator = jwtGenerator;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResponseDto?> AuthenticateWithGoogleAsync(string googleToken)
        {
            if (!googleToken.Contains('.'))
                return null;

            string? googleUserId = null;
            string? email = null;
            string? nickname = null;

            try
            {
                var clientId = _configuration["Google:ClientId"];
                if (string.IsNullOrEmpty(clientId))
                {
                    Console.WriteLine("[AuthService] Google:ClientId not configured.");
                    return null;
                }

                Console.WriteLine($"[AuthService] Validating Google token with ClientId: {clientId[..20]}...");
                Console.WriteLine($"[AuthService] Server UTC now: {DateTime.UtcNow:O}");

                // Decode JWT payload to inspect iat
                var parts = googleToken.Split('.');
                if (parts.Length >= 2)
                {
                    var paddedPayload = parts[1].PadRight((parts[1].Length + 3) / 4 * 4, '=').Replace('-', '+').Replace('_', '/');
                    var jsonBytes = Convert.FromBase64String(paddedPayload);
                    var jsonStr = System.Text.Encoding.UTF8.GetString(jsonBytes);
                    Console.WriteLine($"[AuthService] Token payload: {jsonStr[..Math.Min(200, jsonStr.Length)]}");
                }

                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId },
                    IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
                    ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(googleToken, validationSettings);
                googleUserId = payload.Subject;
                email = payload.Email;
                nickname = payload.Name;

                Console.WriteLine($"[AuthService] Token valid. GoogleId: {googleUserId}, Email: {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthService] Token validation failed: [{ex.GetType().Name}] {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[AuthService] Inner: [{ex.InnerException.GetType().Name}] {ex.InnerException.Message}");
                return null;
            }

            if (string.IsNullOrEmpty(googleUserId))
                return null;

            var user = await _userRepository.GetByGoogleIdAsync(googleUserId);

            if (user == null)
            {
                user = new User
                {
                    GoogleId = googleUserId,
                    Email = email ?? $"{googleUserId}@gmail.com",
                    Nickname = nickname ?? $"User_{Guid.NewGuid().ToString("N")[..8]}"
                };
                await _userRepository.AddAsync(user);
            }

            return await GenerateTokensAsync(user);
        }

        public async Task<AuthResponseDto> GenerateTokensAsync(User user)
        {
            var (accessToken, expiry) = _jwtGenerator.GenerateAccessToken(user);
            var refreshTokenString = _jwtGenerator.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _unitOfWork.CommitAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                AccessTokenExpiry = expiry,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Nickname = user.Nickname,
                    AvatarUrl = user.AvatarUrl,
                    Bio = user.Bio,
                    Status = user.Status,
                    LastSeen = user.LastSeen
                }
            };
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshTokenString)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenString);

            if (refreshToken == null || refreshToken.IsRevoked || refreshToken.IsUsed || refreshToken.ExpiresAt < DateTime.UtcNow)
                return null;

            refreshToken.IsUsed = true;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
            if (user == null) return null;

            var (newAccessToken, expiry) = _jwtGenerator.GenerateAccessToken(user);
            var newRefreshTokenString = _jwtGenerator.GenerateRefreshToken();

            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            await _refreshTokenRepository.AddAsync(newRefreshToken);
            await _unitOfWork.CommitAsync();

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString,
                AccessTokenExpiry = expiry,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Nickname = user.Nickname,
                    AvatarUrl = user.AvatarUrl,
                    Bio = user.Bio,
                    Status = user.Status,
                    LastSeen = user.LastSeen
                }
            };
        }

        public async Task RevokeRefreshTokenAsync(string refreshTokenString)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenString);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                await _refreshTokenRepository.UpdateAsync(refreshToken);
                await _unitOfWork.CommitAsync();
            }
        }
    }
}
