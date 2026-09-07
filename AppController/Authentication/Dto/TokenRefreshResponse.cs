namespace AppController.Authentication.Dto;

public record TokenRefreshResponse
{
    public string RefreshToken { get; init; } = null!;
    public string Token { get; init; } = null!;
}