using opn_chat.Application.DTOs;
using opn_chat.Domain.Entities;

namespace opn_chat.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> AuthenticateWithGoogleAsync(string googleToken);
        Task<AuthResponseDto> GenerateTokensAsync(User user);
        Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }

    public interface IJwtGenerator
    {
        (string token, DateTime expiry) GenerateAccessToken(User user);
        string GenerateRefreshToken();
    }
}
