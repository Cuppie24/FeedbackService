namespace Application.EntityServices.Feedback.Dto;

public class FeedbackFilter
{
    public List<int> Ids { get; set; } = [];
    public List<int> StatusIds { get; set; } = [];
    public List<int> TypeIds { get; set; } = [];
    public List<int> TagIds { get; set; } = [];
    public List<int> AssigneeIds { get; set; } = [];
    public List<int> SystemIds { get; set; } = [];
    /// <summary>
    /// Search by title
    /// </summary>
    public string? Search { get; set; } = null;
    public DateTime? FromDate { get; set; } = null;
    public DateTime? ToDate { get; set; } = null;
}