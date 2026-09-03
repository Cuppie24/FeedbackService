namespace Domain.Entities;

public class FeedbackAttachment
{
    public int Id { get; set; }
    public string? Path { get; set; }
    public int MessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public FeedbackMessage? Message { get; set; } = null!;
}