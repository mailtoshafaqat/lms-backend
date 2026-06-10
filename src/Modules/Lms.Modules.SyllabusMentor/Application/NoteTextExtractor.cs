using System.Text;
using Lms.Shared.Content;
using Lms.Shared.Storage;
using UglyToad.PdfPig;

namespace Lms.Modules.SyllabusMentor.Application;

public sealed class NoteTextExtractor
{
    private readonly IFileStorage _storage;

    public NoteTextExtractor(IFileStorage storage) => _storage = storage;

    public async Task<string> ExtractAsync(NoteIngestItem note, CancellationToken ct)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(note.ContentHtml))
            parts.Add(TextChunker.StripHtml(note.ContentHtml));

        if (!string.IsNullOrWhiteSpace(note.StorageKey))
        {
            var fileText = await ReadStorageAsync(note.StorageKey, ct);
            if (!string.IsNullOrWhiteSpace(fileText))
                parts.Add(fileText);
        }

        return string.Join("\n\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private async Task<string> ReadStorageAsync(string key, CancellationToken ct)
    {
        await using var stream = await _storage.OpenAsync(key, ct);
        if (stream is null) return string.Empty;

        var ext = Path.GetExtension(key).ToLowerInvariant();
        if (ext is ".txt" or ".md" or ".html" or ".htm")
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var raw = await reader.ReadToEndAsync(ct);
            return ext is ".html" or ".htm" ? TextChunker.StripHtml(raw) : raw;
        }

        if (ext == ".pdf")
            return ExtractPdfText(stream);

        return string.Empty;
    }

    private static string ExtractPdfText(Stream stream)
    {
        try
        {
            using var doc = PdfDocument.Open(stream);
            var sb = new StringBuilder();
            foreach (var page in doc.GetPages())
                sb.AppendLine(page.Text);
            return sb.ToString().Trim();
        }
        catch
        {
            return string.Empty;
        }
    }
}
