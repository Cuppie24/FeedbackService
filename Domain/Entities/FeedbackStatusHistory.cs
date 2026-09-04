namespace Domain.Entities;

public class FeedbackStatusHistory
{
    public int Id { get; set; }
    public int FeedbackId { get; set; }
    public int StatusId { get; set; }
    public int ChangerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public Feedback? Feedback { get; set; }
    public FeedbackStatus? Status { get; set; }
    public User? Changer { get; set; }
}