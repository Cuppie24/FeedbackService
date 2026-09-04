using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Feedback
{
    public int Id { get; set; }
    [MaxLength(500)] public string Title { get; set; } = null!;
    public int AssigneeId { get; set; }
    public int UserId { get; set; }
    public int TypeId { get; set; }
    public int StatusId { get; set; }
    public int SystemId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public User? Assignee { get; set; }
    public User? User { get; set; }
    public FeedbackType? Type { get; set; }
    public FeedbackStatus? Status { get; set; }
    public AppSystem? System { get; set; }
    
}