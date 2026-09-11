namespace Application.EntityServices.Feedback.Dto;

public record FeedbackUpdateDto
{
    public int Id { get; init; }
    public string? Title { get; init; }
    /// <summary>
    /// Id of the user who is trying to update 
    /// </summary>
    public int UserId { get; init; }
}