using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lms.Modules.SyllabusMentor.Domain;
using Lms.Modules.SyllabusMentor.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Content;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Lms.Shared.Mentor;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lms.Modules.SyllabusMentor.Application;

public sealed class SyllabusMentorService : ISyllabusMentorService
{
    private readonly SyllabusMentorDbContext _db;
    private readonly ICourseScopeReader _scope;
    private readonly IContentNotesReader _notes;
    private readonly IEnrollmentReader _enrollments;
    private readonly ISyllabusMentorGate _gate;
    private readonly ITenantContext _tenant;
    private readonly NoteTextExtractor _extractor;
    private readonly SyllabusMentorOptions _options;
    private readonly IHttpClientFactory _http;

    public SyllabusMentorService(
        SyllabusMentorDbContext db,
        ICourseScopeReader scope,
        IContentNotesReader notes,
        IEnrollmentReader enrollments,
        ISyllabusMentorGate gate,
        ITenantContext tenant,
        NoteTextExtractor extractor,
        IOptions<SyllabusMentorOptions> options,
        IHttpClientFactory http)
    {
        _db = db;
        _scope = scope;
        _notes = notes;
        _enrollments = enrollments;
        _gate = gate;
        _tenant = tenant;
        _extractor = extractor;
        _options = options.Value;
        _http = http;
    }

    public async Task<AskResponse> AskAsync(
        Guid userId,
        string role,
        AskRequest request,
        CancellationToken ct = default)
    {
        var config = await _gate.GetConfigAsync(_tenant.TenantId, ct);
        if (!config.Enabled)
            throw new InvalidOperationException("Syllabus Mentor is not enabled for this institute.");

        var question = request.Question?.Trim() ?? string.Empty;
        if (question.Length < 3)
            throw new ArgumentException("Question is too short.");

        if (request.TopicId is null && request.SubjectId is null)
            throw new ArgumentException("Provide topicId or subjectId.");

        if (request.TopicId is not null && request.SubjectId is not null)
            throw new ArgumentException("Provide only one of topicId or subjectId.");

        var language = NormalizeLanguage(request.Language);
        var (scopeLabel, topicIds, bundleId) = await ResolveScopeAsync(request, ct);

        await EnsureSyllabusAccessAsync(userId, role, bundleId, ct);

        var tokens = Tokenize(question);
        var chunks = await _db.KnowledgeChunks.AsNoTracking()
            .Where(c =>
                (request.TopicId != null && c.TopicId == request.TopicId) ||
                (request.SubjectId != null && c.SubjectId == request.SubjectId))
            .ToListAsync(ct);

        var ranked = RankChunks(chunks, tokens, language);

        if (ranked.Count == 0)
        {
            var emptyMsg = language == "ur"
                ? "اس موضوع کے لیے ابھی کوئی نوٹس انڈیکس نہیں ہوئے۔ اپنے استاد سے نوٹس اپ لوڈ کرنے کو کہیں، پھر دوبارہ کوشش کریں۔"
                : "No notes are indexed for this scope yet. Ask your teacher to upload notes, then try again.";
            return new AskResponse(emptyMsg, language, scopeLabel, true, Array.Empty<CitationDto>());
        }

        var citations = ranked.Select(x => new CitationDto(
            x.Chunk.SourceType,
            x.Chunk.SourceTitle,
            x.Chunk.TopicId,
            Truncate(x.Chunk.Text, 220))).ToList();

        var context = string.Join("\n\n", ranked.Select((x, i) =>
            $"[{i + 1}] ({x.Chunk.SourceTitle})\n{x.Chunk.Text}"));

        var answer = await ComposeAnswerAsync(question, context, language, scopeLabel, ct);

        return new AskResponse(answer, language, scopeLabel, true, citations);
    }

    public async Task<IngestResponse> IngestAsync(IngestRequest request, CancellationToken ct = default)
    {
        IReadOnlyList<Guid> topicIds;

        if (request.TopicId is not null)
        {
            topicIds = [request.TopicId.Value];
            await _db.KnowledgeChunks
                .Where(c => c.TopicId == request.TopicId)
                .ExecuteDeleteAsync(ct);
        }
        else if (request.SubjectId is not null)
        {
            topicIds = await _scope.GetTopicIdsForSubjectAsync(request.SubjectId.Value, ct);
            await _db.KnowledgeChunks
                .Where(c => c.SubjectId == request.SubjectId)
                .ExecuteDeleteAsync(ct);
        }
        else
        {
            throw new ArgumentException("Provide topicId or subjectId.");
        }

        var notes = await _notes.GetNotesForTopicsAsync(topicIds, ct);
        var subjectId = request.SubjectId;
        if (subjectId is null && request.TopicId is not null)
        {
            var scope = await _scope.GetTopicScopeAsync(request.TopicId.Value, ct);
            subjectId = scope?.SubjectId;
        }

        var count = 0;
        foreach (var note in notes)
        {
            var text = await _extractor.ExtractAsync(note, ct);
            if (string.IsNullOrWhiteSpace(text)) continue;

            var pieces = TextChunker.Split(text, _options.ChunkSize);
            for (var i = 0; i < pieces.Count; i++)
            {
                _db.KnowledgeChunks.Add(new KnowledgeChunk
                {
                    TenantId = _tenant.TenantId,
                    TopicId = note.TopicId,
                    SubjectId = subjectId,
                    SourceType = "note",
                    SourceId = note.NoteId,
                    SourceTitle = note.Title,
                    Text = pieces[i],
                    ChunkIndex = i
                });
                count++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return new IngestResponse(count, $"Indexed {count} chunks from {notes.Count} notes.");
    }

    private async Task<(string ScopeLabel, IReadOnlyList<Guid> TopicIds, Guid BundleId)> ResolveScopeAsync(
        AskRequest request,
        CancellationToken ct)
    {
        if (request.TopicId is not null)
        {
            var scope = await _scope.GetTopicScopeAsync(request.TopicId.Value, ct)
                ?? throw new InvalidOperationException("Topic not found.");
            return ($"Topic: {scope.TopicTitle}", [scope.TopicId], scope.BundleId);
        }

        var sub = await _scope.GetSubjectScopeAsync(request.SubjectId!.Value, ct)
            ?? throw new InvalidOperationException("Subject not found.");
        var topicIds = await _scope.GetTopicIdsForSubjectAsync(sub.SubjectId, ct);
        return ($"Subject: {sub.SubjectTitle}", topicIds, sub.BundleId);
    }

    private async Task EnsureSyllabusAccessAsync(Guid userId, string role, Guid bundleId, CancellationToken ct)
    {
        if (role is Roles.SuperAdmin or Roles.InstituteAdmin or Roles.Teacher)
            return;

        var bundles = await _enrollments.GetActiveBundleIdsAsync(userId, ct);
        if (!bundles.Contains(bundleId))
            throw new UnauthorizedAccessException("You are not enrolled in this course scope.");
    }

    private async Task<string> ComposeAnswerAsync(
        string question,
        string context,
        string language,
        string scopeLabel,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_options.OpenAiApiKey))
        {
            var system = language == "ur"
                ? $"آپ ایک امتحانی تیاری کے مینٹر ہیں۔ صرف فراہم کردہ نوٹس سے جواب دیں۔ اسکوپ: {scopeLabel}۔ اردو میں واضح قدم بہ قدم جواب دیں۔"
                : $"You are an exam-prep mentor. Answer ONLY from the provided notes. Scope: {scopeLabel}. Be concise and step-by-step. Cite note titles inline.";

            var user = $"Notes:\n{context}\n\nQuestion: {question}";
            var llm = await CallOpenAiAsync(system, user, ct);
            if (!string.IsNullOrWhiteSpace(llm)) return llm;
        }

        var intro = language == "ur"
            ? $"یہ جواب آپ کے سلیبس کی نوٹس سے ہے ({scopeLabel}):"
            : $"Based on your syllabus notes ({scopeLabel}):";

        var body = string.Join(
            language == "ur" ? "\n\n" : "\n\n",
            context.Split("\n\n", StringSplitOptions.RemoveEmptyEntries).Take(3));

        return $"{intro}\n\n{body}";
    }

    private async Task<string?> CallOpenAiAsync(string system, string user, CancellationToken ct)
    {
        try
        {
            var client = _http.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAiApiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = _options.OpenAiModel,
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = user }
                },
                temperature = 0.2
            }), Encoding.UTF8, "application/json");

            var res = await client.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content").GetString();
        }
        catch
        {
            return null;
        }
    }

    private List<(KnowledgeChunk Chunk, int Score)> RankChunks(
        IReadOnlyList<KnowledgeChunk> chunks,
        IReadOnlyList<string> tokens,
        string language)
    {
        var ranked = chunks
            .Select(c => (Chunk: c, Score: ScoreChunk(c.Text, tokens)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(_options.MaxChunks)
            .ToList();

        if (ranked.Count == 0 && chunks.Count > 0 && language == "ur")
        {
            ranked = chunks
                .OrderBy(c => c.ChunkIndex)
                .Take(_options.MaxChunks)
                .Select(c => (Chunk: c, Score: 1))
                .ToList();
        }

        return ranked;
    }

    private static int ScoreChunk(string text, IReadOnlyList<string> tokens)
    {
        var lower = text.ToLowerInvariant();
        return tokens.Sum(t => lower.Contains(t, StringComparison.Ordinal) ? 1 : 0);
    }

    private static IReadOnlyList<string> Tokenize(string question) =>
        question.ToLowerInvariant()
            .Split([' ', '?', '.', ',', '!', ';', ':', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .Distinct()
            .ToList();

    private static string NormalizeLanguage(string? lang) =>
        string.Equals(lang, "ur", StringComparison.OrdinalIgnoreCase) ? "ur" : "en";

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";
}
