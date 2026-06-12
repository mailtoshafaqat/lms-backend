using Lms.Shared.Common;

namespace Lms.Modules.Assessments.Application;

public interface IQuizAdminService
{
    Task<PagedResult<QuestionSearchHitDto>> SearchQuestionsAsync(
        string query, int page, int pageSize, CancellationToken ct = default);

    Task<AdminQuizDto?> GetAdminQuizAsync(Guid topicId, CancellationToken ct = default);
    Task<Result<AdminQuestionDto>> AddQuestionAsync(Guid topicId, CreateQuestionRequest req, CancellationToken ct = default);
    Task<bool> DeleteQuestionAsync(Guid questionId, CancellationToken ct = default);
    Task<Result<AdminQuestionDto>> UpdateQuestionAsync(Guid questionId, UpdateQuestionRequest req, CancellationToken ct = default);
    Task<Result<bool>> UpdateQuizTitleAsync(Guid topicId, UpdateQuizTitleRequest req, CancellationToken ct = default);
    Task<Result<bool>> ReorderQuestionsAsync(Guid topicId, ReorderQuestionsRequest req, CancellationToken ct = default);
    Task<Result<AdminQuizDto>> UpdateQuizSettingsAsync(
        Guid topicId, UpdateQuizSettingsRequest req, CancellationToken ct = default);

    Task<Result<AdminQuizDto>> PublishResultsAsync(Guid topicId, CancellationToken ct = default);

    Task<QuizAnalyticsDto?> GetQuizAnalyticsAsync(Guid topicId, CancellationToken ct = default);

    Task<Result<McqImportPreviewDto>> PreviewMcqImportAsync(
        IReadOnlyList<McqImportRowInput> rows, CancellationToken ct = default);

    Task<Result<McqImportResultDto>> ImportMcqAsync(
        Guid topicId, McqImportRequest req, CancellationToken ct = default);

    Task<AdminUnitQuizDto?> GetUnitQuizAsync(Guid unitId, string quizType, CancellationToken ct = default);

    Task<Result<AdminUnitQuizDto>> UpdateUnitQuizSettingsAsync(
        Guid unitId, string quizType, UpdateQuizSettingsRequest req, CancellationToken ct = default);
}
