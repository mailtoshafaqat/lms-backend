namespace Lms.Modules.Assessments.Application;



public sealed record MockExamTopicDto(

    Guid TopicId,

    string TopicTitle,

    int QuestionCount,

    int Order);



public sealed record MockExamSectionDto(

    Guid Id,

    string Title,

    int SortOrder,

    int? SectionTimeLimitMinutes,

    IReadOnlyList<MockExamTopicDto> Topics);



public sealed record AdminMockExamDto(

    Guid Id,

    Guid BundleId,

    string BundleTitle,

    Guid SubjectId,

    string SubjectTitle,

    string Title,

    string? Description,

    int TimeLimitMinutes,

    decimal MarksPerCorrect,

    decimal PenaltyPerWrong,

    DateTime? AvailableFromUtc,

    DateTime? AvailableUntilUtc,

    bool IsPublished,

    bool IsArchived,

    string ResultVisibility,

    bool ShowExplanations,

    DateTime? ResultsPublishedAtUtc,

    bool NotifyTeachersOnBatchComplete,

    int BatchCompleteThresholdPercent,

    IReadOnlyList<MockExamSectionDto> Sections,

    IReadOnlyList<MockExamTopicDto> Topics);



public sealed record MockExamTopicInput(Guid TopicId, int QuestionCount);



public sealed record MockExamSectionInput(

    string Title,

    int SortOrder,

    int? SectionTimeLimitMinutes,

    IReadOnlyList<MockExamTopicInput> Topics);



public sealed record CreateMockExamRequest(

    Guid SubjectId,

    string Title,

    string? Description,

    int TimeLimitMinutes,

    decimal? MarksPerCorrect = null,

    decimal? PenaltyPerWrong = null,

    DateTime? AvailableFromUtc = null,

    DateTime? AvailableUntilUtc = null,

    bool IsPublished = false,

    string? ResultVisibility = null,

    bool? ShowExplanations = null,

    bool? NotifyTeachersOnBatchComplete = null,

    int? BatchCompleteThresholdPercent = null,

    IReadOnlyList<MockExamSectionInput>? Sections = null,

    IReadOnlyList<MockExamTopicInput>? Topics = null);



public sealed record UpdateMockExamRequest(

    string Title,

    string? Description,

    int TimeLimitMinutes,

    decimal? MarksPerCorrect = null,

    decimal? PenaltyPerWrong = null,

    DateTime? AvailableFromUtc = null,

    DateTime? AvailableUntilUtc = null,

    bool IsPublished = false,

    string? ResultVisibility = null,

    bool? ShowExplanations = null,

    bool? NotifyTeachersOnBatchComplete = null,

    int? BatchCompleteThresholdPercent = null,

    IReadOnlyList<MockExamSectionInput>? Sections = null,

    IReadOnlyList<MockExamTopicInput>? Topics = null);



public sealed record MockExamSummaryDto(

    Guid Id,

    Guid BundleId,

    string BundleTitle,

    Guid SubjectId,

    string SubjectTitle,

    string Title,

    string? Description,

    int TimeLimitMinutes,

    decimal MarksPerCorrect,

    decimal PenaltyPerWrong,

    DateTime? AvailableFromUtc,

    DateTime? AvailableUntilUtc,

    string AvailabilityStatus,

    int TotalQuestions,

    DateTime? AccessExpiresAtUtc,

    ActiveMockAttemptDto? ActiveAttempt);



public sealed record ActiveMockAttemptDto(

    Guid AttemptId,

    DateTime StartedAtUtc,

    DateTime? ExpiresAtUtc);



public sealed record MockExamSectionNavDto(

    string Title,

    int SortOrder,

    int StartQuestionNumber,

    int QuestionCount);



public sealed record MockExamQuestionDto(

    Guid Id,

    string Stem,

    IReadOnlyList<string> Options,

    int Order,

    string? SectionTitle = null);



public sealed record StartMockAttemptResultDto(

    Guid AttemptId,

    DateTime StartedAtUtc,

    DateTime? ExpiresAtUtc,

    IReadOnlyList<MockExamQuestionDto> Questions,

    IReadOnlyList<Guid> FlaggedQuestionIds,

    IReadOnlyList<MockExamSectionNavDto> Sections);



public sealed record MockExamQuestionResultDto(

    Guid QuestionId,

    string Stem,

    IReadOnlyList<string> Options,

    string CorrectKey,

    string? SelectedKey,

    bool IsCorrect,

    string? Explanation);



public sealed record MockExamRankDto(

    int Rank,

    int TotalAttempts,

    decimal Percentile);



public sealed record MockExamAttemptResultDto(

    Guid AttemptId,

    decimal Score,

    int Total,

    int CorrectCount,

    int WrongCount,

    decimal MarksPerCorrect,

    decimal PenaltyPerWrong,

    bool ResultsVisible,

    string ResultsStatus,

    string? ResultsMessage,

    DateTime? ResultsAvailableAtUtc,

    bool ShowExplanations,

    MockExamRankDto? Rank,

    IReadOnlyList<MockExamQuestionResultDto> Questions);



public sealed record MockExamLeaderboardRowDto(

    int Rank,

    Guid UserId,

    string Name,

    decimal Score,

    int CorrectCount,

    int WrongCount,

    DateTime SubmittedAtUtc,

    bool IsMe);



public sealed record MockExamLeaderboardDto(

    IReadOnlyList<MockExamLeaderboardRowDto> Rows,

    int TotalAttempts);



public sealed record SubmitMockAttemptRequest(

    IReadOnlyList<SubmitAnswerDto> Answers,

    Guid? AttemptId,

    IReadOnlyList<Guid>? FlaggedQuestionIds = null);



public sealed record SubmitAnswerDto(Guid QuestionId, string SelectedKey);



public sealed record SetMockExamArchivedRequest(bool IsArchived);

