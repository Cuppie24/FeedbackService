using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Feedback
{
    public int Id { get; set; }
    [MaxLength(500)] public string Title { get; set; } = null!;
    
}