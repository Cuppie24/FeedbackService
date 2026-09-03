namespace Domain.Entities;

public class FeedbackVote
{
    public int Id { get; set; }
    public int FeedbackId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public Feedback? Feedback { get; set; }
    public User? User { get; set; }
}