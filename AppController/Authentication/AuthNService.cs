using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AppController.Authentication.Dto;
using Application.Crypto;
using Application.EntityServices.RefreshToken;
using Application.EntityServices.User;
using Domain.Entities;
using Microsoft.Extensions.Options;

namespace AppController.Authentication;

public class AuthNService(
    IUserRepo userRepo,
    IRefreshTokenRepo refreshTokenRepo,
    ICryptoService cryptoService,
    IConfiguration config,
    ILogger<AuthNService> logger,
    IOptions<JwtOptions> jwtOptions,
    IPasswordValidator passwordValidator
) : IAuthNService
{
    public async Task<AuthNTokens> AuthenticateAsync(AuthRequest request, string? refreshToken = null)
    {
        var user = await userRepo.GetUserByUsername(request.Username);
        if (user is null)
        {
            logger.LogError("User {RequestUsername} not found on authenticate try", request.Username);
            return null;
        }
        
        var isPasswordValid = await passwordValidator.ValidatePassword(request);
        if (isPasswordValid)
        {
            string newRefreshToken;
            newRefreshToken = await RotateRefreshTokens(refreshToken, user);
            var token = GenerateToken(user);
            var result = new AuthNTokens
            {
                RefreshToken = newRefreshToken,
                Token = token
            }
        }
        logger.LogWarning("Failed to authenticate user {Username}", request.Username);
        return null;
    }

    public bool IsAuthenticated(string token)
    {
        var tokenParts = token.Split('.');
        if (tokenParts.Length != 3)
        {
            logger.LogError("Invalid token parts length {TokenPartsLength}", tokenParts.Length);
            return false;
        }

        var issuerSigningKey = config.GetValue<string>("Jwt:IssuerSigningKey");
        if (string.IsNullOrWhiteSpace(issuerSigningKey))
        {
            logger.LogError("Missing issuerSigningKey");
            return false;
        }

        var hash = cryptoService.Hs256Hash(string.Concat(tokenParts[0], ".", tokenParts[1]), issuerSigningKey);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hash),
               Encoding.UTF8.GetBytes(tokenParts[2])))
        {
            logger.LogError("Invalid token signature on authenticate try");
            return false;
        }

        return true;
    }

    // Rotation lives in the repository so the create + revoke run in one DB transaction.
    public Task<string?> RotateRefreshTokens(string? refreshToken, int userId) =>
        refreshTokenRepo.RotateRefreshAsync(refreshToken, userId);
    
    private string GenerateToken(User user)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new
        {
            sub = user.Id,
            name = user.Username,
            iss = jwtOptions.Value.Issuer,
            aud = jwtOptions.Value.Audience,
            exp = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.ExpireMinutes).ToUnixTimeSeconds()
        };

        var headerJson = JsonSerializer.Serialize(header);
        var payloadJson = JsonSerializer.Serialize(payload);

        var base64Header = cryptoService.Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson ?? ""));
        var base64Payload = cryptoService.Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson ?? ""));
        var base64Signature = cryptoService.Hs256Hash($"{base64Header}.{base64Payload}", jwtOptions.Value.IssuerSigningKey);
        return $"{base64Header}.{base64Payload}.{base64Signature}";
    }
}