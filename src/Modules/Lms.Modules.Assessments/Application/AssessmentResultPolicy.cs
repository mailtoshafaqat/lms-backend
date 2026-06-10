using Lms.Modules.Assessments.Domain;

namespace Lms.Modules.Assessments.Application;

public static class AssessmentResultPolicy
{
    public const string StatusVisible = "Visible";
    public const string StatusPendingAfterClose = "PendingAfterClose";
    public const string StatusPendingManual = "PendingManual";

    public static string ResolveStatus(
        ResultVisibilityMode mode,
        DateTime? availableUntilUtc,
        DateTime? resultsPublishedAtUtc,
        DateTime nowUtc)
    {
        return mode switch
        {
            ResultVisibilityMode.Immediate => StatusVisible,
            ResultVisibilityMode.AfterClose when availableUntilUtc is not null && nowUtc > availableUntilUtc.Value
                => StatusVisible,
            ResultVisibilityMode.AfterClose => StatusPendingAfterClose,
            ResultVisibilityMode.ManualPublish when resultsPublishedAtUtc is not null => StatusVisible,
            ResultVisibilityMode.ManualPublish => StatusPendingManual,
            _ => StatusVisible
        };
    }

    public static bool AreResultsVisible(string status) => status == StatusVisible;

    public static string Message(string status, DateTime? availableUntilUtc, DateTime? resultsPublishedAtUtc)
    {
        return status switch
        {
            StatusPendingAfterClose => availableUntilUtc is null
                ? "Your answers were submitted. Results will be available after the test window closes."
                : $"Your answers were submitted. Results will be available after {availableUntilUtc.Value:u}.",
            StatusPendingManual when resultsPublishedAtUtc is not null
                => "Results have been published. Refresh this page to view your score.",
            StatusPendingManual
                => "Your answers were submitted. Your teacher will publish results when ready.",
            _ => string.Empty
        };
    }

    public static DateTime? ResultsAvailableAtUtc(
        ResultVisibilityMode mode,
        DateTime? availableUntilUtc,
        DateTime? resultsPublishedAtUtc)
    {
        return mode switch
        {
            ResultVisibilityMode.AfterClose => availableUntilUtc,
            ResultVisibilityMode.ManualPublish => resultsPublishedAtUtc,
            _ => null
        };
    }

    public static ResultVisibilityMode? ParseMode(string? value) =>
        string.IsNullOrWhiteSpace(value) || !Enum.TryParse<ResultVisibilityMode>(value, true, out var mode)
            ? null
            : mode;

    public static IReadOnlyList<QuestionResultDto> GateQuestionResults(
        bool visible,
        bool showExplanations,
        IReadOnlyList<QuestionResultDto> fullResults)
    {
        if (!visible) return Array.Empty<QuestionResultDto>();
        if (showExplanations) return fullResults;

        return fullResults
            .Select(q => q with { Explanation = null })
            .ToList();
    }

    public static IReadOnlyList<MockExamQuestionResultDto> GateMockQuestionResults(
        bool visible,
        bool showExplanations,
        IReadOnlyList<MockExamQuestionResultDto> fullResults)
    {
        if (!visible) return Array.Empty<MockExamQuestionResultDto>();
        if (showExplanations) return fullResults;

        return fullResults
            .Select(q => q with { Explanation = null })
            .ToList();
    }
}
