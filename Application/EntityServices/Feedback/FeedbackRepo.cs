using Application.EntityServices.Feedback.Dto;

namespace Application.EntityServices.Feedback;

public interface IFeedbackRepo
{
    Task<Domain.Entities.Feedback?> GetAsync(int id);
    Task<Domain.Entities.Feedback?> CreateAsync(Domain.Entities.Feedback feedback);
    Task UpdateAsync(FeedbackUpdateDto updateDto);
    Task DeleteAsync(int id);
}