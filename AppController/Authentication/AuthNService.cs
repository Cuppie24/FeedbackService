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
    public async Task<AuthNTokens?> AuthenticateAsync(AuthRequest request, string? refreshToken = null)
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
            var newRefreshTokenText = GenerateRefreshToken();
            var newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenText,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.RefreshExpireMinutes)
            };
            var newRefreshTokenId = await refreshTokenRepo.CreateWithRevokeAsync(newRefreshToken, refreshToken);
            if (newRefreshTokenId is null)
                throw new InvalidOperationException("Failed to create new refresh token");
            
            var newJwtToken = GenerateJwtToken(user);
            var result = new AuthNTokens
            {
                RefreshToken = newRefreshTokenText,
                JwtToken = newJwtToken
            };
            return result;
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

    public async Task<AuthNTokens?> RotateRefreshTokens(string oldRefreshToken)
    {
        var newRefreshTokenText = GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenText,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.RefreshExpireMinutes)
        };
        var createdRefreshToken = await refreshTokenRepo.RotateRefreshAsync(newRefreshToken, oldRefreshToken);
        if (createdRefreshToken is null)
            return null;
        var user = await userRepo.GetAsync(createdRefreshToken.UserId);
        if (user is null)
            throw new InvalidOperationException("Failed to get user for refresh token");
        
        return new AuthNTokens()
        {
            RefreshToken = newRefreshToken.Token,
            JwtToken = GenerateJwtToken(user)
        };
    }

    private string GenerateJwtToken(User user)
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

    private string GenerateRefreshToken()
    {
        return cryptoService.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }
}