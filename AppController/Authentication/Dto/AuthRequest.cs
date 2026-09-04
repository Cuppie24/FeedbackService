namespace AppController.Authentication.Dto;

public record AuthRequest
{
    public int UserId { get; set; }
    public string Username { get; init; } = null!;
    public string Password { get; init; } = null!;
}