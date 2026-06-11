using System.Text.Json;
using Lms.Modules.Assessments.Domain;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Courses;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Assessments.Application;

internal static class QuizQuestionAssembler
{
    public static QuestionDifficulty? ParseDifficulty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<QuestionDifficulty>(value, ignoreCase: true, out var parsed) ? parsed : null;
    }

    public static IReadOnlyList<Guid> DeserializeGuidList(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? Array.Empty<Guid>()
            : JsonSerializer.Deserialize<List<Guid>>(json) ?? [];

    public static string SerializeGuidList(IEnumerable<Guid> ids) =>
        JsonSerializer.Serialize(ids.Distinct().ToList());

    public static IReadOnlyList<string> DistinctDifficultyNames(IEnumerable<Question> questions) =>
        questions
            .Select(q => q.Difficulty.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d)
            .ToList();

    public static IEnumerable<Question> ApplyDifficultyFilter(
        IEnumerable<Question> questions,
        QuestionDifficulty? quizFilter,
        QuestionDifficulty? studentFilter)
    {
        var effective = studentFilter ?? quizFilter;
        return effective is null
            ? questions
            : questions.Where(q => q.Difficulty == effective);
    }

    public static async Task<IReadOnlyList<Question>> GetTopicQuestionsAsync(
        AssessmentsDbContext db,
        Quiz quiz,
        QuestionDifficulty? studentFilter,
        CancellationToken ct)
    {
        await db.Entry(quiz).Collection(q => q.Questions).LoadAsync(ct);
        return ApplyDifficultyFilter(quiz.Questions, quiz.DifficultyFilter, studentFilter)
            .OrderBy(q => q.Order)
            .ToList();
    }

    public static async Task<IReadOnlyList<Question>> AssembleUnitQuestionsAsync(
        AssessmentsDbContext db,
        ICourseScopeReader scope,
        Guid unitId,
        QuizType type,
        QuestionDifficulty? quizFilter,
        QuestionDifficulty? studentFilter,
        CancellationToken ct)
    {
        var topicIds = await scope.GetTopicIdsForUnitAsync(unitId, ct);
        if (topicIds.Count == 0) return Array.Empty<Question>();

        var quizzes = await db.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .Where(q => q.TopicId != null
                && topicIds.Contains(q.TopicId.Value)
                && q.Type == QuizType.DailyPracticeTest)
            .ToListAsync(ct);

        var questions = quizzes.SelectMany(q => q.Questions).AsEnumerable();
        if (type == QuizType.PyqTest)
            questions = questions.Where(q => q.IsPyq);

        questions = ApplyDifficultyFilter(questions, quizFilter, studentFilter);

        return questions
            .OrderBy(q => q.Order)
            .ThenBy(q => q.Id)
            .ToList();
    }

    public static async Task<IReadOnlyList<Question>> ResolveAttemptQuestionsAsync(
        AssessmentsDbContext db,
        ICourseScopeReader scope,
        Quiz quiz,
        Attempt? attempt,
        QuestionDifficulty? studentFilter,
        CancellationToken ct)
    {
        if (quiz.IsAssembledQuiz)
        {
            if (attempt is not null && !string.IsNullOrWhiteSpace(attempt.QuestionIdsJson))
            {
                var ids = DeserializeGuidList(attempt.QuestionIdsJson);
                if (ids.Count == 0) return Array.Empty<Question>();

                var loaded = await db.Questions.Where(q => ids.Contains(q.Id)).ToListAsync(ct);
                return ids
                    .Select(id => loaded.FirstOrDefault(q => q.Id == id))
                    .Where(q => q is not null)
                    .Cast<Question>()
                    .ToList();
            }

            if (quiz.UnitId is null) return Array.Empty<Question>();
            return await AssembleUnitQuestionsAsync(
                db, scope, quiz.UnitId.Value, quiz.Type, quiz.DifficultyFilter, studentFilter, ct);
        }

        return await GetTopicQuestionsAsync(db, quiz, studentFilter, ct);
    }

    public static IReadOnlyList<QuizQuestionDto> MapQuestions(IReadOnlyList<Question> questions) =>
        questions
            .Select((q, i) => new QuizQuestionDto(q.Id, q.Stem, DeserializeOptions(q.OptionsJson), i + 1))
            .ToList();

    private static IReadOnlyList<string> DeserializeOptions(string json) =>
        JsonSerializer.Deserialize<List<string>>(json) ?? [];
}
