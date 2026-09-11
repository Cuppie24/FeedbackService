using Application.EntityServices.Feedback.Dto;
using CSharpFunctionalExtensions;

namespace Application.EntityServices.Feedback;

public class FeedbackService(IFeedbackRepo feedbackRepo) : IFeedbackService
{
    public async Task<Result<Domain.Entities.Feedback?>> GetAsync(int id) =>
        Result.Success(await feedbackRepo.GetAsync(id));

    public async Task<Result<List<Domain.Entities.Feedback>>> GetAsync(FeedbackFilter filter) =>
        Result.Success(await feedbackRepo.GetAsync(filter));

    public async Task<Result<int>> CreateAsync(Domain.Entities.Feedback feedback)
    {
        var result = await feedbackRepo.CreateAsync(feedback);
        return result is null ? Result.Failure<int>("Feedback not created") : Result.Success(result.Id);
    }

    public async Task<Result> UpdateAsync(FeedbackUpdateDto updateDto)
    {
        return await EnsureFeedbackExists(updateDto.Id).Bind(async feedbackToUpdate =>
            await EnsureFeedbackBelongsToUser(feedbackToUpdate.Id, updateDto.UserId).Bind(async _ =>
            {
                await feedbackRepo.UpdateAsync(updateDto);
                return Result.Success();
            }));
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await EnsureFeedbackExists(id).Bind(async feedbackToDelete =>
            await EnsureFeedbackBelongsToUser(feedbackToDelete.Id, feedbackToDelete.UserId).Bind(async
                _ =>
            {
                await feedbackRepo.DeleteAsync(id);
                return Result.Success();
            })
        );
    }

    private async Task<Result<Domain.Entities.Feedback>> EnsureFeedbackExists(int id)
    {
        var feedback = await feedbackRepo.GetAsync(id);
        return feedback is null ? Result.Failure<Domain.Entities.Feedback>("Feedback not found") : Result.Success(feedback);
    }

    private async Task<Result<bool>> EnsureFeedbackBelongsToUser(int id, int userId)
    {
        throw new NotImplementedException();
    }
}