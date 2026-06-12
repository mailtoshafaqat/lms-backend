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

public sealed record SubjectAccuracyDto(
    Guid SubjectId,
    string SubjectTitle,
    int Accuracy,
    int QuizzesCompleted);

public sealed record WeeklyScoreDto(
    string DayLabel,
    int Accuracy,
    int Attempts);

public sealed record BundleProgressDto(
    Guid BundleId,
    string BundleTitle,
    int TopicsCompleted,
    int TopicsTotal,
    int PercentComplete);

public sealed record DashboardOverviewDto(
    int OverallAccuracy,
    int AccuracyChangeThisWeek,
    int McqsAttemptedThisMonth,
    int? InstituteRank,
    int InstituteStudentCount,
    int PracticeStreakDays,
    IReadOnlyList<SubjectAccuracyDto> SubjectAccuracy,
    IReadOnlyList<WeeklyScoreDto> WeeklyTrend,
    IReadOnlyList<BundleProgressDto> BundleProgress,
    SubjectAccuracyDto? WeakestSubject);
