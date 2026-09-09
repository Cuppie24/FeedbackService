namespace Application.EntityServices.RefreshToken;

public interface IRefreshTokenRepo
{
    Task<Domain.Entities.RefreshToken?> GetAsync(string refreshToken);
    Task<Domain.Entities.RefreshToken?> GetAsync(int id);
    Task RevokeAsync(int id, int replacedByTokenId);
    Task<int?> CreateAsync(Domain.Entities.RefreshToken refreshToken);
    /// <summary>
    /// Creates a new refresh token and revokes the previous one. Doesn't fail if the previous token is expired
    /// </summary>
    Task<int?> CreateWithRevokeAsync(Domain.Entities.RefreshToken refreshToken, string? previousRefreshToken);
    /// <summary>
    /// Creates a new refresh token and revokes the previous one. Fails if the previous token is expired
    /// </summary>
    Task<Domain.Entities.RefreshToken?> RotateRefreshAsync(Domain.Entities.RefreshToken newToken, string oldTokenText);
}