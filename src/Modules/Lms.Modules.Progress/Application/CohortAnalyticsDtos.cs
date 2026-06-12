namespace Lms.Modules.Progress.Application;

public sealed record CohortAnalyticsOverviewDto(
    Guid BundleId,
    string BundleTitle,
    int EnrolledStudents,
    int AvgCompletionPercent,
    int AvgQuizAccuracy,
    int TotalCertificatesIssued);

public sealed record CohortStudentRowDto(
    Guid UserId,
    string StudentName,
    int TopicsCompleted,
    int TopicsTotal,
    int CompletionPercent,
    int AvgQuizAccuracy,
    int VideosWatched,
    int VideosTotal,
    DateTime? LastActiveAt);
