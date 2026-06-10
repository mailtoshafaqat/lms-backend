using System.Text.RegularExpressions;

namespace Lms.Modules.SyllabusMentor.Application;

internal static class TextChunker
{
    public static IReadOnlyList<string> Split(string text, int chunkSize)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();

        var chunks = new List<string>();
        for (var i = 0; i < text.Length; i += chunkSize)
        {
            var len = Math.Min(chunkSize, text.Length - i);
            chunks.Add(text.Substring(i, len).Trim());
        }

        return chunks.Where(c => c.Length > 20).ToList();
    }

    public static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = Regex.Replace(text, "\\s+", " ").Trim();
        return text;
    }
}
