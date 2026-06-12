namespace Lms.Modules.Content.Application;

public sealed record CreateLectureRequest(string Title, string? Url, string? StorageKey, int DurationSec, int Order);
public sealed record CreateNoteRequest(string Title, string? ContentHtml, string? StorageKey, int Order);
