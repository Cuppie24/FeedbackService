namespace Application.EntityServices.RefreshToken;

public interface IRefreshTokenRepo
{
    Task<Domain.Entities.RefreshToken?> GetAsync(string refreshToken);
    Task<Domain.Entities.RefreshToken?> GetAsync(int id);
    Task RevokeAsync(int id, int replacedByTokenId);
    Task<int?> CreateAsync(Domain.Entities.RefreshToken? refreshToken, int? previousTokenToRevokeId = null);
    Task<string?> RotateRefreshAsync(string? refreshToken, int userId);
}