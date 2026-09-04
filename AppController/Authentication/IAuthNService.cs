using AppController.Authentication.Dto;

namespace AppController.Authentication;

public interface IAuthNService
{
    /// <returns>Jwt token on successful authentication</returns>
    Task<string?> AuthenticateAsync(AuthRequest request);
    bool IsAuthenticated(string token);
    Task<string> Refresh(string refreshToken);
}