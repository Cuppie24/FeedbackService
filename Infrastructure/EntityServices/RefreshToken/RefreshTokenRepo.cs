using System.Security.Cryptography;
using Application.EntityServices.RefreshToken;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Infrastructure.EntityServices.RefreshToken;

public class RefreshTokenRepo(
    MySqlDataSource dataSource,
    ILogger<RefreshTokenRepo> logger,
    IConfiguration config) : IRefreshTokenRepo
{
    private const string SelectColumns =
        "id, token, user_id, expires_at, replaced_by_token_id, is_revoked, revoked_at, created_at, updated_at";

    private const string InsertReturningIdSql =
        """
        INSERT INTO refresh_tokens
            (token, user_id, expires_at, replaced_by_token_id, is_revoked, revoked_at, created_at, updated_at)
        VALUES
            (@Token, @UserId, @ExpiresAt, @ReplacedByTokenId, @IsRevoked, @RevokedAt, @CreatedAt, @UpdatedAt);
        SELECT LAST_INSERT_ID();
        """;

    private const string RevokeByIdSql =
        """
        UPDATE refresh_tokens
        SET is_revoked = 1,
            revoked_at = @now,
            replaced_by_token_id = @replacedByTokenId,
            updated_at = @now
        WHERE id = @id AND is_revoked = 0
        """;

    // Refresh-token lifetime lives under the "Jwt" config section alongside the
    // access-token settings; Infrastructure can't see AppController's JwtOptions,
    // so it is read straight from configuration and validated at construction.
    private readonly int _refreshExpiresMinutes =
        int.TryParse(config["Jwt:RefreshExpiresMinutes"], out var minutes) && minutes > 0
            ? minutes
            : throw new InvalidOperationException(
                "Config value 'Jwt:RefreshExpiresMinutes' is required and must be a positive integer");

    public async Task<Domain.Entities.RefreshToken?> GetAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            logger.LogError("Attempt to get a refresh token by an empty token value");
            return null;
        }

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Domain.Entities.RefreshToken>(
            $"SELECT {SelectColumns} FROM refresh_tokens WHERE token = @refreshToken",
            new { refreshToken });
    }

    public async Task<Domain.Entities.RefreshToken?> GetAsync(int id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Domain.Entities.RefreshToken>(
            $"SELECT {SelectColumns} FROM refresh_tokens WHERE id = @id",
            new { id });
    }

    public async Task RevokeAsync(int id, int replacedByTokenId)
    {
        var now = DateTime.UtcNow;

        await using var connection = await dataSource.OpenConnectionAsync();
        var affected = await connection.ExecuteAsync(RevokeByIdSql, new { id, replacedByTokenId, now });

        if (affected == 0)
            logger.LogWarning(
                "Refresh token {TokenId} was not revoked: it does not exist or is already revoked", id);
    }

    public async Task<int?> CreateAsync(Domain.Entities.RefreshToken? refreshToken, int? previousTokenToRevokeId = null)
    {
        if (refreshToken is null)
        {
            logger.LogError("Attempt to create a null refresh token");
            return null;
        }

        var now = DateTime.UtcNow;
        refreshToken.CreatedAt = now;
        refreshToken.UpdatedAt = now;

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var newId = await connection.ExecuteScalarAsync<int>(InsertReturningIdSql, refreshToken, transaction);

            if (previousTokenToRevokeId.HasValue)
            {
                var revoked = await connection.ExecuteAsync(
                    RevokeByIdSql,
                    new { id = previousTokenToRevokeId.Value, replacedByTokenId = newId, now }, transaction);

                if (revoked == 0)
                    logger.LogWarning(
                        "Previous refresh token {TokenId} was not revoked on rotation: it does not exist or is already revoked",
                        previousTokenToRevokeId);
            }

            await transaction.CommitAsync();
            return newId;
        }
        catch (MySqlException e)
        {
            await transaction.RollbackAsync();
            logger.LogError(e, "Failed to create a refresh token for user {UserId}", refreshToken.UserId);
            return null;
        }
    }

    public async Task<string?> RotateRefreshAsync(string? refreshToken, int userId)
    {
        var newTokenText = GenerateTokenValue();

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;

            // Validate (and row-lock) the token being rotated out inside the same
            // transaction: a concurrent rotation of the same token blocks on the
            // FOR UPDATE and then sees it already revoked instead of racing us.
            Domain.Entities.RefreshToken? oldToken = null;
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                oldToken = await connection.QueryFirstOrDefaultAsync<Domain.Entities.RefreshToken>(
                    $"SELECT {SelectColumns} FROM refresh_tokens WHERE token = @refreshToken FOR UPDATE",
                    new { refreshToken }, transaction);

                if (oldToken is null)
                {
                    logger.LogError("Refresh token not found on rotation");
                    await transaction.RollbackAsync();
                    return null;
                }
                if (oldToken.UserId != userId)
                {
                    logger.LogWarning(
                        "Refresh token {TokenId} belongs to another user; rotation denied", oldToken.Id);
                    await transaction.RollbackAsync();
                    return null;
                }
                if (oldToken.ExpiresAt < now.AddMinutes(-5))
                {
                    logger.LogInformation("Refresh token {TokenId} is expired; rotation denied", oldToken.Id);
                    await transaction.RollbackAsync();
                    return null;
                }
                if (oldToken.IsRevoked)
                {
                    logger.LogWarning("Refresh token {TokenId} is already revoked; rotation denied", oldToken.Id);
                    await transaction.RollbackAsync();
                    return null;
                }
            }

            var newToken = new Domain.Entities.RefreshToken
            {
                Token = newTokenText,
                UserId = userId,
                ExpiresAt = now.AddMinutes(_refreshExpiresMinutes),
                ReplacedByTokenId = null,
                IsRevoked = false,
                RevokedAt = null,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var newId = await connection.ExecuteScalarAsync<int>(InsertReturningIdSql, newToken, transaction);

            if (oldToken is not null)
            {
                var revoked = await connection.ExecuteAsync(
                    RevokeByIdSql,
                    new { id = oldToken.Id, replacedByTokenId = newId, now }, transaction);

                if (revoked == 0)
                {
                    // Should be unreachable while we hold the FOR UPDATE lock and
                    // already checked is_revoked; treat as a lost race and abort.
                    logger.LogError(
                        "Refresh token {TokenId} could not be revoked during rotation; rolling back", oldToken.Id);
                    await transaction.RollbackAsync();
                    return null;
                }
            }

            await transaction.CommitAsync();
            return newTokenText;
        }
        catch (MySqlException e)
        {
            await transaction.RollbackAsync();
            logger.LogError(e, "Failed to rotate refresh token for user {UserId}", userId);
            return null;
        }
    }

    private static string GenerateTokenValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
