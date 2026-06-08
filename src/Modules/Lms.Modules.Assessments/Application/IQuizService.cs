using Lms.Shared.Common;

namespace Lms.Modules.Assessments.Application;

public interface IQuizService
{
    Task<QuizDto?> GetByTopicAsync(Guid topicId, CancellationToken ct = default);
    Task<QuizDto?> GetAsync(Guid quizId, CancellationToken ct = default);
    Task<Result<AttemptResultDto>> SubmitAsync(Guid quizId, Guid userId, SubmitAttemptRequest request, CancellationToken ct = default);
}
