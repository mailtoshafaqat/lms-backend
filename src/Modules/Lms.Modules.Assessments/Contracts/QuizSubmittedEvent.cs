using Lms.Shared.Events;

namespace Lms.Modules.Assessments.Contracts;

/// <summary>Published when a student submits a quiz. Future modules subscribe to react:
/// Grades (recalculate), Leaderboard (update rank), Mistake Diary (record wrong answers).
/// The Assessments module does not know or care who listens.</summary>
public sealed record QuizSubmittedEvent(
    Guid AttemptId,
    Guid TenantId,
    Guid UserId,
    Guid QuizId,
    Guid TopicId,
    string QuizTitle,
    int Score,
    int Total,
    IReadOnlyList<Guid> WrongQuestionIds) : IEvent;
