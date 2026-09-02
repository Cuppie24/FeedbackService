namespace Application.Authentication.Dto;

public record AuthRequest
{
    public string Username { get; init; } = null!;
    public string Password { get; init; } = null!;
}