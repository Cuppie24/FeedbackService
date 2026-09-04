namespace Domain.Entities;

public class FeedbackTag
{
    public int Id { get; set; }
    public int TagId { get; set; }
    public int FeedbackId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public Tag? Tag { get; set; }
    public Feedback? Feedback { get; set; }
}
