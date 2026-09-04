using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = null!;
    [MaxLength(50)] public string Code { get; set; } = null!;
}
