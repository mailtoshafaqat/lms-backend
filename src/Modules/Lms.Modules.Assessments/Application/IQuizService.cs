using Lms.Shared.Common;

namespace Lms.Modules.Assessments.Application;

public interface IQuizService
{
    Task<QuizDto?> GetByTopicAsync(
        Guid topicId, Guid? userId, string? difficulty = null, CancellationToken ct = default);
    Task<QuizDto?> GetByUnitAsync(
        Guid unitId, string quizType, Guid? userId, string? difficulty = null, CancellationToken ct = default);
    Task<QuizDto?> GetAsync(Guid quizId, Guid? userId, string? difficulty = null, CancellationToken ct = default);
    Task<Result<StartAttemptResultDto>> StartAttemptAsync(Guid quizId, Guid userId, CancellationToken ct = default);
    Task<Result<AttemptResultDto>> SubmitAsync(Guid quizId, Guid userId, SubmitAttemptRequest request, CancellationToken ct = default);
    Task<Result<AttemptResultDto>> GetAttemptResultAsync(Guid quizId, Guid userId, CancellationToken ct = default);
}
