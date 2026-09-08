using System.Text;
using Application.EntityServices.RefreshToken;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Infrastructure.EntityServices.RefreshToken;

public class RefreshTokenRepo(MySqlDataSource dataSource, ILogger<RefreshTokenRepo> logger) : IRefreshTokenRepo
{
    private const string SelectColumns =
        "id, token, user_id, expires_at, replaced_by_token_id, is_revoked, revoked_at, created_at, updated_at";

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
        var affected = await connection.ExecuteAsync(
            $"""
             UPDATE refresh_tokens
             SET is_revoked = 1,
                 revoked_at = @now,
                 replaced_by_token_id = @replacedByTokenId,
                 updated_at = @now
             WHERE id = @id AND is_revoked = 0
             """,
            new { id, replacedByTokenId, now });

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
            var newId = await connection.ExecuteScalarAsync<int>(
                """
                INSERT INTO refresh_tokens
                    (token, user_id, expires_at, replaced_by_token_id, is_revoked, revoked_at, created_at, updated_at)
                VALUES
                    (@Token, @UserId, @ExpiresAt, @ReplacedByTokenId, @IsRevoked, @RevokedAt, @CreatedAt, @UpdatedAt);
                SELECT LAST_INSERT_ID();
                """,
                refreshToken, transaction);

            if (previousTokenToRevokeId.HasValue)
            {
                var revoked = await connection.ExecuteAsync(
                    """
                    UPDATE refresh_tokens
                    SET is_revoked = 1,
                        revoked_at = @now,
                        replaced_by_token_id = @newId,
                        updated_at = @now
                    WHERE id = @previousTokenToRevokeId AND is_revoked = 0
                    """,
                    new { now, newId, previousTokenToRevokeId }, transaction);

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
}
