using Application.Authentication.Dto;

namespace Application.Authentication;

public interface IAuthService
{
    bool IsAuthenticated(AuthRequest request);
}