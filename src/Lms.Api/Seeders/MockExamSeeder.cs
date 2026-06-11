using System.Text.Json;
using Lms.Modules.Assessments.Domain;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Modules.Courses.Infrastructure;
using Lms.Modules.Enrollment.Infrastructure;
using Lms.Modules.Identity.Infrastructure;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using EnrollmentEntity = Lms.Modules.Enrollment.Domain.Enrollment;

namespace Lms.Api.Seeders;

/// <summary>Seeds an MDCAT-style mock exam with blueprint sections, scoring, and ranked demo attempts.</summary>
public static class MockExamSeeder
{
    public const string DemoExamTitle = "MDCAT Mock Test #1 (Demo)";

    public static async Task SeedAsync(
        AssessmentsDbContext assessmentsDb,
        CoursesDbContext coursesDb,
        IdentityDbContext identityDb,
        EnrollmentDbContext enrollmentDb,
        CancellationToken ct = default)
    {
        if (await assessmentsDb.MockExams.IgnoreQueryFilters()
                .AnyAsync(m => m.Title == DemoExamTitle, ct))
            return;

        var tenantId = TenantContext.DefaultTenantId;
        var bundle = await coursesDb.Bundles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Title == "MDCAT Premium 2026", ct);
        if (bundle is null) return;

        var subject = await coursesDb.Subjects.IgnoreQueryFilters()
            .Include(s => s.Units).ThenInclude(u => u.Topics)
            .FirstOrDefaultAsync(s => s.Title == "MDCAT Full Length" && s.BundleId == bundle.Id, ct);
        if (subject is null) return;

        var orderedUnits = subject.Units.OrderBy(u => u.Order).ToList();
        var sectionTopics = orderedUnits
            .Select(u => u.Topics.OrderBy(t => t.Order).First())
            .ToList();
        if (sectionTopics.Count < 5) return;

        await EnsureTopicQuestionPoolsAsync(assessmentsDb, sectionTopics, tenantId, ct);

        var exam = new MockExam
        {
            TenantId = tenantId,
            BundleId = bundle.Id,
            BundleTitle = bundle.Title,
            SubjectId = subject.Id,
            SubjectTitle = subject.Title,
            Title = DemoExamTitle,
            Description = "Full-length MDCAT practice with section blueprint and +4/-1 marking.",
            TimeLimitMinutes = 150,
            MarksPerCorrect = 4m,
            PenaltyPerWrong = 1m,
            IsPublished = true,
            ResultVisibility = ResultVisibilityMode.Immediate,
            ShowExplanations = true
        };

        for (var i = 0; i < sectionTopics.Count; i++)
        {
            var topic = sectionTopics[i];
            var section = new MockExamSection
            {
                TenantId = tenantId,
                Title = orderedUnits[i].Title,
                SortOrder = i + 1
            };
            var row = new MockExamTopic
            {
                TenantId = tenantId,
                TopicId = topic.Id,
                TopicTitle = topic.Title,
                QuestionCount = 2,
                Order = 1
            };
            section.Topics.Add(row);
            exam.Topics.Add(row);
            exam.Sections.Add(section);
        }

        assessmentsDb.MockExams.Add(exam);
        await assessmentsDb.SaveChangesAsync(ct);

        var students = await identityDb.Users.IgnoreQueryFilters()
            .Where(u => u.Email == IdentitySeeder.Student1Email
                        || u.Email == IdentitySeeder.Student2Email
                        || u.Email == IdentitySeeder.Student3Email)
            .ToListAsync(ct);

        foreach (var student in students)
        {
            if (!await enrollmentDb.Enrollments.IgnoreQueryFilters()
                    .AnyAsync(e => e.UserId == student.Id && e.BundleId == bundle.Id, ct))
            {
                enrollmentDb.Enrollments.Add(new EnrollmentEntity
                {
                    TenantId = tenantId,
                    UserId = student.Id,
                    BundleId = bundle.Id,
                    BundleTitle = bundle.Title,
                    PricePaid = bundle.Price,
                    EnrolledAt = DateTime.UtcNow.AddDays(-7),
                    ExpiresAt = DateTime.UtcNow.AddYears(1)
                });
            }
        }

        await enrollmentDb.SaveChangesAsync(ct);

        var questionIds = new List<Guid>();
        foreach (var section in exam.Sections.OrderBy(s => s.SortOrder))
        {
            foreach (var topic in section.Topics.OrderBy(t => t.Order))
            {
                var quiz = await assessmentsDb.Quizzes.IgnoreQueryFilters()
                    .Include(q => q.Questions)
                    .FirstAsync(q => q.TopicId == topic.TopicId, ct);
                questionIds.AddRange(quiz.Questions.OrderBy(q => q.Order).Take(topic.QuestionCount).Select(q => q.Id));
            }
        }

        var submittedAt = DateTime.UtcNow.AddHours(-2);
        var scores = new[] { (8, 2), (6, 3), (5, 4) };

        for (var i = 0; i < students.Count && i < scores.Length; i++)
        {
            var (correct, wrong) = scores[i];
            var answers = new Dictionary<Guid, string>();
            for (var q = 0; q < questionIds.Count; q++)
            {
                var question = await assessmentsDb.Questions.IgnoreQueryFilters()
                    .FirstAsync(x => x.Id == questionIds[q], ct);
                if (q < correct)
                    answers[question.Id] = question.CorrectKey;
                else if (q < correct + wrong)
                    answers[question.Id] = question.CorrectKey == "0" ? "1" : "0";
            }

            assessmentsDb.MockExamAttempts.Add(new MockExamAttempt
            {
                TenantId = tenantId,
                MockExamId = exam.Id,
                UserId = students[i].Id,
                QuestionIdsJson = JsonSerializer.Serialize(questionIds),
                AnswersJson = JsonSerializer.Serialize(answers),
                CorrectCount = correct,
                WrongCount = wrong,
                Score = correct * exam.MarksPerCorrect - wrong * exam.PenaltyPerWrong,
                Total = questionIds.Count,
                StartedAt = submittedAt.AddMinutes(-150 + i * 5),
                SubmittedAt = submittedAt.AddMinutes(i * 5),
                ExpiresAtUtc = submittedAt.AddMinutes(-150 + i * 5).AddMinutes(exam.TimeLimitMinutes)
            });
        }

        await assessmentsDb.SaveChangesAsync(ct);
    }

    private static async Task EnsureTopicQuestionPoolsAsync(
        AssessmentsDbContext db,
        IReadOnlyList<Lms.Modules.Courses.Domain.Topic> topics,
        Guid tenantId,
        CancellationToken ct)
    {
        foreach (var topic in topics)
        {
            var quiz = await db.Quizzes.IgnoreQueryFilters()
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.TopicId == topic.Id, ct);
            if (quiz is null)
            {
                quiz = new Quiz
                {
                    TenantId = tenantId,
                    TopicId = topic.Id,
                    Title = $"{topic.Title} — Practice",
                    Type = QuizType.DailyPracticeTest
                };
                db.Quizzes.Add(quiz);
            }

            var needed = 10 - quiz.Questions.Count;
            for (var i = 0; i < needed; i++)
            {
                var order = quiz.Questions.Count + 1;
                quiz.Questions.Add(new Question
                {
                    TenantId = tenantId,
                    Stem = $"MDCAT {topic.Title} question {order}?",
                    OptionsJson = JsonSerializer.Serialize(new[] { "Option A", "Option B", "Option C", "Option D" }),
                    CorrectKey = (order % 4).ToString(),
                    Explanation = $"Concept check for {topic.Title}.",
                    Order = order
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
