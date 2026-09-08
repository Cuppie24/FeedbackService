using System.Security.Cryptography;
using System.Text;
using AppController.Authentication.Dto;
using Application.EntityServices.User;

namespace AppController.Authentication;

public class PasswordValidator(IUserRepo userRepo,
    ILogger<PasswordValidator> logger) : IPasswordValidator
{
    public async Task<bool> ValidatePassword(AuthRequest request)
    {
        var user = await userRepo.GetUserByUsername(request.Username);
        if (user is null)
        {
            logger.LogError("User {RequestUsername} not found on authenticate try", request.Username);
            return false;
        }
        
        var hash = Md5Hash(request.Password + request.Username?.ToUpper());
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(user.PasswordHash));
    }
    
    public string Md5Hash(string text)
    {
        var hasher = MD5.Create();
        var bytes = hasher.ComputeHash(Encoding.Default.GetBytes(text));
        var result = new StringBuilder();
        foreach (var b in bytes)
            result.Append(b.ToString("x2"));
        return result.ToString();
    }
}