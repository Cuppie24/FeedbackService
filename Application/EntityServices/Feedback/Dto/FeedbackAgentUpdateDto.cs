namespace Application.EntityServices.Feedback.Dto;

public record FeedbackAgentUpdateDto : FeedbackUpdateDto
{
    public int? StatusId { get; init; }
    public int? TypeId { get; init; }
}