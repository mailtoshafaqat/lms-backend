using System.Text.Json;
using Lms.Modules.Assessments.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Assessments.Infrastructure;

/// <summary>Seeds a sample DPT (3 MCQs) per topic (dev only). Topics passed in by the host
/// to keep the module decoupled.</summary>
public static class AssessmentSeeder
{
    public static async Task SeedAsync(
        AssessmentsDbContext db,
        IEnumerable<(Guid TopicId, string Title)> topics,
        CancellationToken ct = default)
    {
        if (await db.Quizzes.IgnoreQueryFilters().AnyAsync(ct)) return;

        var tenantId = TenantContext.DefaultTenantId;

        foreach (var (topicId, title) in topics)
        {
            var quiz = new Quiz
            {
                TenantId = tenantId,
                TopicId = topicId,
                Title = $"{title} — Daily Practice Test",
                Type = QuizType.DailyPracticeTest
            };

            for (var i = 1; i <= 3; i++)
            {
                quiz.Questions.Add(new Question
                {
                    TenantId = tenantId,
                    Stem = $"Sample question {i} about {title}?",
                    OptionsJson = JsonSerializer.Serialize(new[] { "Option A", "Option B", "Option C", "Option D" }),
                    CorrectKey = ((i - 1) % 4).ToString(),
                    Explanation = $"The correct choice tests a key concept in {title}.",
                    Order = i
                });
            }

            db.Quizzes.Add(quiz);
        }

        await db.SaveChangesAsync(ct);
    }
}
