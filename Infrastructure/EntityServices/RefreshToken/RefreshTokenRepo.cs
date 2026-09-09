using System.Security.Cryptography;
using System.Transactions;
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
    private readonly int _refreshExpireMinutes =
        int.TryParse(config["Jwt:RefreshExpireMinutes"], out var minutes) && minutes > 0
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

    public Task RevokeAsync(int id, int replacedByTokenId) =>
        Revoke(id, replacedByTokenId);

    public Task<int?> CreateAsync(Domain.Entities.RefreshToken refreshToken) =>
        Create(refreshToken);

    private async Task Revoke(int id, int replacedByTokenId,
        MySqlConnection? exConnection = null,
        MySqlTransaction? exTransaction = null)
    {
        var now = DateTime.UtcNow;


        var connection = exConnection;
        var ownsConnection = connection is null;
        if (ownsConnection)
            connection = await dataSource.OpenConnectionAsync();
        try
        {
            var affected = await connection!.ExecuteAsync(RevokeByIdSql,
                new { id, replacedByTokenId, now },
                transaction: exTransaction);

            if (affected == 0)
                logger.LogWarning(
                    "Refresh token {TokenId} was not revoked: it does not exist or is already revoked", id);
        }
        finally
        {
            if (ownsConnection)
                await connection!.DisposeAsync();
        }
    }

    private async Task<int?> Create(Domain.Entities.RefreshToken refreshToken,
        MySqlConnection? exConnection = null,
        MySqlTransaction? transaction = null)
    {
        var now = DateTime.UtcNow;
        refreshToken.CreatedAt = now;
        refreshToken.UpdatedAt = now;

        var connection = exConnection;
        var ownsConnection = connection is null;
        if (ownsConnection)
            connection = await dataSource.OpenConnectionAsync();
        try
        {
            var newId = await connection!.ExecuteScalarAsync<int>(InsertReturningIdSql, refreshToken, transaction);
            return newId;
        }
        catch (MySqlException e)
        {
            logger.LogError(e, "Failed to create a refresh token for user {UserId}", refreshToken.UserId);
            return null;
        }
        finally
        {
            if (ownsConnection)
                await connection!.DisposeAsync();
        }
    }


    public async Task<int?> CreateWithRevokeAsync(Domain.Entities.RefreshToken refreshToken,
        string? previousRefreshToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var resultId = await Create(refreshToken, connection, transaction);
            if (!string.IsNullOrWhiteSpace(previousRefreshToken))
            {
                var oldRefresh = await GetAsync(previousRefreshToken);
                if (oldRefresh is null)
                    logger.LogWarning("Previous refresh token not found on revoke for user {UserId}",
                        refreshToken.UserId);
                else if (oldRefresh.IsRevoked)
                    logger.LogWarning(
                        "Previous refresh token is already revoked on revoke for user {UserId}. Skipping revoke",
                        refreshToken.UserId);
                else if (oldRefresh.UserId != refreshToken.UserId)
                {
                    logger.LogDebug(
                        "Previous refresh token {OldRefreshTokenId} belongs to user {OldUserId} on revoke for user {UserId}. Skipping revoke",
                        oldRefresh.Id, oldRefresh.UserId, refreshToken.UserId);
                    logger.LogError(
                        "Previous refresh token belongs to another user on revoke for user {UserId}. Skipping revoke",
                        refreshToken.UserId);
                }
                else if (resultId.HasValue)
                    await Revoke(oldRefresh.Id, resultId.Value, connection, transaction);
            }

            await transaction.CommitAsync();
            return resultId;
        }
        catch (MySqlException e)
        {
            logger.LogError(e, "Failed to create a refresh token for user {UserId}", refreshToken.UserId);
            await transaction.RollbackAsync();
            return null;
        }
    }

    public async Task<Domain.Entities.RefreshToken?> RotateRefreshAsync(Domain.Entities.RefreshToken newToken,
        string oldTokenText)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;

            // Validate (and row-lock) the token being rotated out inside the same
            // transaction: a concurrent rotation of the same token blocks on the
            // FOR UPDATE and then sees it already revoked instead of racing us.
            Domain.Entities.RefreshToken? oldToken = null;
            if (string.IsNullOrWhiteSpace(oldTokenText))
                throw new InvalidOperationException("Refresh token text is required");
            oldToken = await connection.QueryFirstOrDefaultAsync<Domain.Entities.RefreshToken>(
                $"SELECT {SelectColumns} FROM refresh_tokens WHERE token = @oldTokenText FOR UPDATE",
                new { oldTokenText }, transaction);

            if (oldToken is null)
            {
                logger.LogError("Refresh token not found on rotation");
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

            newToken.UserId = oldToken.UserId;
            newToken.IsRevoked = false;
            newToken.RevokedAt = null;
            newToken.ReplacedByTokenId = null;
            newToken.CreatedAt = now;
            newToken.UpdatedAt = now;

            newToken.Id = await Create(newToken, connection, transaction) ??
                          throw new InvalidOperationException("Failed to create new refresh token");

            await Revoke(oldToken.Id, newToken.Id, connection, transaction);

            await transaction.CommitAsync();
            return newToken;
        }
        catch (MySqlException e)
        {
            await transaction.RollbackAsync();
            logger.LogError(e, "Failed to rotate refresh token");
            return null;
        }
    }
}