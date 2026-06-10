using Lms.Shared.Entities;

namespace Lms.Modules.LiveClasses.Domain;

/// <summary>Records when a student joined a live class session.</summary>
public sealed class LiveClassAttendance : TenantEntity
{
    public Guid LiveClassId { get; set; }
    public LiveClass? LiveClass { get; set; }

    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime JoinedAtUtc { get; set; }
}
