namespace Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? ReplacedByTokenId { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public User? User { get; set; }
    public RefreshToken? ReplacedByToken { get; set; }
}
