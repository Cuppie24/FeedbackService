using System.Security.Cryptography;
using System.Text;
using AppController.Authentication.Dto;
using Application.Crypto;
using Application.EntityServices.User;
using Domain.Entities;

namespace AppController.Authentication;

public class AuthNService(
    IUserRepository userRepository,
    ICryptoService cryptoService,
    IConfiguration config,
    ILogger<AuthNService> logger
) : IAuthNService
{
    public async Task<string?> AuthenticateAsync(AuthRequest request)
    {
        var user = await userRepository.GetUser(request.UserId);
        if (user is null)
        {
            logger.LogError("User {RequestUsername} id:{RequestUserId} not found on authenticate try", request.Username,
                request.UserId);
            return null;
        }

        var hash = cryptoService.Md5Hash(request.Password);
        if (hash.Equals(user.PasswordHash))
            return GenerateToken(user);
        logger.LogWarning("Failed to authenticate user {UserId}", request.UserId);
        return null;
    }

    public bool IsAuthenticated(string token)
    {
        var tokenParts = token.Split('.');
        if (tokenParts.Length != 3)
        {
            logger.LogError("Invalid token length {Token}", token);
            return false;
        }

        var issuerSigningKey = config.GetValue<string>("Jwt:IssuerSigningKey");
        if (string.IsNullOrWhiteSpace(issuerSigningKey))
        {
            logger.LogError("Missing issuerSigningKey");
            return false;
        }

        var hash = cryptoService.HmacSha256Hash(string.Concat(tokenParts[0], ".", tokenParts[1]), issuerSigningKey);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hash),
               Encoding.UTF8.GetBytes(tokenParts[2])))
        {
            logger.LogError("Invalid token signature on authenticate try. Token: {Token}", token);
            return false;
        }

        return true;
    }

    public async Task<string> Refresh(string refreshToken)
    {
        throw new NotImplementedException();
    }

    private string GenerateToken(User user)
    {
        throw new NotImplementedException();
    }
}