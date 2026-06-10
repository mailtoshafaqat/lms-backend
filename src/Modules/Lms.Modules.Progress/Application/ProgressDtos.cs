namespace Lms.Modules.Progress.Application;

public sealed record GradeDto(
    Guid QuizId,
    Guid TopicId,
    string QuizTitle,
    int Score,
    int Total,
    int Percentage,
    DateTime SubmittedAt);

public sealed record LeaderboardRowDto(
    int Rank,
    Guid UserId,
    string Name,
    int Points,
    bool IsMe);

public sealed record SubjectQuizResultDto(
    Guid QuizId,
    Guid TopicId,
    string QuizTitle,
    int BestScore,
    int Total,
    int Percentage,
    DateTime SubmittedAt);

public sealed record StudentSubjectProgressDto(
    Guid UserId,
    string StudentName,
    int QuizzesCompleted,
    int AveragePercentage,
    IReadOnlyList<SubjectQuizResultDto> Results);

public sealed record SubjectProgressDto(
    Guid SubjectId,
    string SubjectTitle,
    IReadOnlyList<StudentSubjectProgressDto> Students);

public sealed record StudentDoubtSummaryDto(
    int OpenCount,
    int ResolvedCount,
    DateTime? LastActivityAt);

public sealed record StudentMistakeSummaryDto(
    int UnresolvedCount,
    int TotalWrongAttempts,
    DateTime? LastSeenAt);

public sealed record StudentDetailDto(
    Guid UserId,
    string StudentName,
    Guid SubjectId,
    string SubjectTitle,
    int QuizzesCompleted,
    int AveragePercentage,
    IReadOnlyList<SubjectQuizResultDto> Grades,
    StudentDoubtSummaryDto Doubts,
    StudentMistakeSummaryDto Mistakes,
    DateTime? LastActiveAt);
