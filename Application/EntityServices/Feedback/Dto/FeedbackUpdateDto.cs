namespace Application.EntityServices.Feedback.Dto;

public record FeedbackUpdateDto
{
    public int Id { get; init; }
    public string? Title { get; init; }
}