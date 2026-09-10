namespace Domain.Entities;

public class Vote
{
    public int Id { get; set; }
    public int FeedbackId { get; set; }
    public int UserId { get; set; }
    public bool IsSubscribed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}