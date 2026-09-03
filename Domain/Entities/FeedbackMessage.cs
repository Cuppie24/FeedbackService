using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class FeedbackMessage
{
    public int Id { get; set; }
    [MaxLength(10000)] public string? Text { get; set; }
    public int UserId { get; set; }
    public int FeedbackId { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public User? User { get; set; } 
    public Feedback? Feedback { get; set; } 
}