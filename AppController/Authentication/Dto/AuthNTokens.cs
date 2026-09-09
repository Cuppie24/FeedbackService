namespace AppController.Authentication.Dto;

public record AuthNTokens
{
    public string RefreshToken { get; init; } = null!;
    public string JwtToken { get; init; } = null!;
}