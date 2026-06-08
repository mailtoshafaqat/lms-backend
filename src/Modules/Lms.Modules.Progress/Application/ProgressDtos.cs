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
