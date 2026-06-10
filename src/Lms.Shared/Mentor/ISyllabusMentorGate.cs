namespace Lms.Shared.Mentor;

public sealed record SyllabusMentorConfig(bool Enabled, string MentorDisplayName);

public interface ISyllabusMentorGate
{
    Task<SyllabusMentorConfig> GetConfigAsync(Guid tenantId, CancellationToken ct = default);
}
