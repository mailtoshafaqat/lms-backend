namespace Lms.Modules.Content.Application;

public sealed record CreateLectureRequest(string Title, string? Url, int DurationSec, int Order);
public sealed record CreateNoteRequest(string Title, string? ContentHtml, int Order);
