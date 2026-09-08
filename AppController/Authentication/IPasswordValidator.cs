using AppController.Authentication.Dto;

namespace AppController.Authentication;

public interface IPasswordValidator
{
    Task<bool> ValidatePassword(AuthRequest request);
}