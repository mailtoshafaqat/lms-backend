namespace Lms.Modules.Content.Application;

/// <summary>Conventions for IFileStorage keys under the content module.</summary>
public static class StoredFilePaths
{
    public const string LecturesPrefix = "lectures/";
    public const string NotesPrefix = "notes/";

    public static bool IsLectureKey(string key) =>
        key.StartsWith(LecturesPrefix, StringComparison.OrdinalIgnoreCase);

    public static bool IsNoteKey(string key) =>
        key.StartsWith(NotesPrefix, StringComparison.OrdinalIgnoreCase);

    public static bool RequiresAuthentication(string key) =>
        IsLectureKey(key) || IsNoteKey(key);
}
