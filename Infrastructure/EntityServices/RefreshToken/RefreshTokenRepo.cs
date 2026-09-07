using Application.EntityServices.RefreshToken;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.EntityServices.RefreshToken;

public class RefreshTokenRepo(AppDbContext db, ILogger<RefreshTokenRepo> logger) : IRefreshTokenRepo
{
    public Task<Domain.Entities.RefreshToken?> GetAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            logger.LogError("Attempt to get a refresh token by an empty token value");
            return Task.FromResult<Domain.Entities.RefreshToken?>(null);
        }

        return db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(token => token.Token == refreshToken);
    }

    public Task<Domain.Entities.RefreshToken?> GetAsync(int id)
    {
        return db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(token => token.Id == id);
    }

    public async Task RevokeAsync(int id, int replacedByTokenId)
    {
        var now = DateTime.UtcNow;
        var affected = await db.RefreshTokens
            .Where(token => token.Id == id && !token.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.IsRevoked, true)
                .SetProperty(token => token.RevokedAt, now)
                .SetProperty(token => token.ReplacedByTokenId, replacedByTokenId)
                .SetProperty(token => token.UpdatedAt, now));

        if (affected == 0)
            logger.LogWarning("Refresh token {TokenId} was not revoked: it does not exist or is already revoked", id);
    }

    public async Task<int?> CreateAsync(Domain.Entities.RefreshToken? refreshToken)
    {
        if (refreshToken is null)
        {
            logger.LogError("Attempt to create a null refresh token");
            return null;
        }

        var now = DateTime.UtcNow;
        refreshToken.CreatedAt = now;
        refreshToken.UpdatedAt = now;

        db.RefreshTokens.Add(refreshToken);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            logger.LogError(e, "Failed to create a refresh token for user {UserId}", refreshToken.UserId);
            db.Entry(refreshToken).State = EntityState.Detached;
            return null;
        }

        return refreshToken.Id;
    }
}
