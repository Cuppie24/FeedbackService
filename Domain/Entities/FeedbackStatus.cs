using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class FeedbackStatus
{
    public int Id { get; set; }
    [MaxLength(50)] public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}