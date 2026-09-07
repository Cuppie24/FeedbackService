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
    IOptions<JwtOptions> jwtOptions
) : IAuthNService
{
    public async Task<string?> AuthenticateAsync(AuthRequest request)
    {
        var user = await userRepo.GetUser(request.UserId);
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
            logger.LogError("Invalid token parts length {TokenPartsLength}", tokenParts.Length);
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
            logger.LogError("Invalid token signature on authenticate try");
            return false;
        }

        return true;
    }

    public async Task<TokenRefreshResponse?> Refresh(string refreshToken)
    {
        var oldRefreshToken = await refreshTokenRepo.GetAsync(refreshToken);
        if (oldRefreshToken is null)
        {
            logger.LogDebug("Refresh token {RefreshToken} not found", refreshToken);
            logger.LogError("Refresh token not found");
            return null;
        }

        if (oldRefreshToken.ExpiresAt < DateTime.UtcNow.AddMinutes(-5))
        {
            logger.LogInformation("Attempt to use expired token. Id: {Id}", oldRefreshToken.Id);
            return null;
        }

        if (oldRefreshToken.IsRevoked)
        {
            logger.LogWarning("Attempt to use revoked token. Id: {Id}", oldRefreshToken.Id);
            return null;
        }

        var user  = await userRepo.GetUser(oldRefreshToken.UserId);
        if (user is null)
        {
            logger.LogError("User {UserId} not found on token {TokenId} refresh try", oldRefreshToken.UserId, oldRefreshToken.Id);
            return null;
        }

        var refreshTokenText = GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            UserId = oldRefreshToken.UserId,
            Token = refreshTokenText,
            ExpiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.RefreshExpiresInMinutes),
            ReplacedByTokenId = null,
            IsRevoked = false,
            RevokedAt = null,
        };
        
        var newId = await refreshTokenRepo.CreateAsync(newRefreshToken);
        if (!newId.HasValue)
        {
            logger.LogError("Error while creating refresh token");
            return null;
        }
            
        await refreshTokenRepo.RevokeAsync(oldRefreshToken.Id, newId.Value);

        var result = new TokenRefreshResponse()
        {
            RefreshToken = refreshTokenText,
            Token = GenerateToken(user)
        };
        return result;
    }
    
    private string GenerateToken(User user)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new
        {
            sub = user.Id,
            name = user.Name,
            iss = jwtOptions.Value.Issuer,
            aud = jwtOptions.Value.Audience,
            exp = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.ExpiresInMinutes).ToUnixTimeSeconds()
        };

        var headerJson = JsonSerializer.Serialize(header);
        var payloadJson = JsonSerializer.Serialize(payload);

        var base64Header = cryptoService.Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson ?? ""));
        var base64Payload = cryptoService.Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson ?? ""));
        var base64Signature = cryptoService.HmacSha256Hash($"{base64Header}.{base64Payload}", jwtOptions.Value.IssuerKey);
        return $"{base64Header}.{base64Payload}.{base64Signature}";
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(randomBytes);
    }
}