namespace Lms.Modules.Assessments.Domain;

/// <summary>When students may see scores and review after submitting.</summary>
public enum ResultVisibilityMode
{
    /// <summary>Score and review immediately after submit (practice / DPT default).</summary>
    Immediate = 0,

    /// <summary>Visible after the availability window closes (<see cref="Quiz.AvailableUntilUtc"/>).</summary>
    AfterClose = 1,

    /// <summary>Teacher publishes results manually (<see cref="Quiz.ResultsPublishedAtUtc"/>).</summary>
    ManualPublish = 2
}
