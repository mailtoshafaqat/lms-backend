namespace Lms.Modules.SyllabusMentor.Application;

public sealed class SyllabusMentorOptions
{
    public const string SectionName = "SyllabusMentor";

    public string? OpenAiApiKey { get; set; }
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    public int MaxChunks { get; set; } = 5;
    public int ChunkSize { get; set; } = 600;
}
