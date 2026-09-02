using Application.Authentication.Dto;

namespace Application.Authentication;

public interface IAuthService
{
    string Authenticate(AuthRequest request);
    bool IsAuthenticated(string token);
}