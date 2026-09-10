namespace Domain.Entities;

public class AppAgent
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public int AgentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
