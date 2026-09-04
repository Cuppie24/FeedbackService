namespace Domain.Entities;

public class AppAgentRole
{
    public int Id { get; set; }
    public int AppAgentId { get; set; }
    public int RoleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public AppAgent? AppAgent { get; set; }
    public Role? Role { get; set; }
}
