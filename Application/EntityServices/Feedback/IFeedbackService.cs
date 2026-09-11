using Application.EntityServices.Feedback.Dto;
using CSharpFunctionalExtensions;

namespace Application.EntityServices.Feedback;

/// <summary>
/// Authorization layer for feedback operations
/// </summary>
public interface IFeedbackService
{
    Task<Result<Domain.Entities.Feedback?>> GetAsync(int id);
    Task<Result<List<Domain.Entities.Feedback>>> GetAsync(FeedbackFilter filter);
    Task<Result<int>> CreateAsync(Domain.Entities.Feedback feedback);
    Task<Result> UpdateAsync(FeedbackUpdateDto updateDto);
    Task<Result> DeleteAsync(int id);
}