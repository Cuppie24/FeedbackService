using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class User
{
    public int Id { get; set; }
    [MaxLength(100)]
    public string? Name { get; set; }
    [MaxLength(100)] 
    public string Username { get; set; } = null!;
    [MaxLength(255)]
    public string Password { get; set; } = null!;
    public bool IsBlocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}