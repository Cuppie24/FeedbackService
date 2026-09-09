using AppController.Authentication.Dto;

namespace AppController.Authentication;

public interface IAuthNService
{
    /// <returns>Jwt token on successful authentication</returns>
    Task<AuthNTokens?> AuthenticateAsync(AuthRequest request, string? refreshToken = null);
    bool IsAuthenticated(string token);
    Task<AuthNTokens?> RotateRefreshTokens(string oldRefreshToken);
}