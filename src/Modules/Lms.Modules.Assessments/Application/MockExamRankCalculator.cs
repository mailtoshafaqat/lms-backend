using Lms.Modules.Assessments.Domain;

namespace Lms.Modules.Assessments.Application;

internal static class MockExamRankCalculator
{
    public static IReadOnlyList<MockExamAttempt> OrderForRanking(IEnumerable<MockExamAttempt> attempts) =>
        attempts
            .Where(a => a.SubmittedAt is not null)
            .OrderByDescending(a => a.Score)
            .ThenBy(a => a.SubmittedAt)
            .ToList();

    public static MockExamRankDto? ComputeRank(MockExamAttempt attempt, IReadOnlyList<MockExamAttempt> ranked)
    {
        if (attempt.SubmittedAt is null || ranked.Count == 0) return null;

        var index = -1;
        for (var i = 0; i < ranked.Count; i++)
        {
            if (ranked[i].Id == attempt.Id)
            {
                index = i;
                break;
            }
        }

        if (index < 0) return null;

        var rank = index + 1;
        var total = ranked.Count;
        var percentile = total <= 1
            ? 100m
            : Math.Round((decimal)(total - rank) / (total - 1) * 100m, 1);

        return new MockExamRankDto(rank, total, percentile);
    }
}
