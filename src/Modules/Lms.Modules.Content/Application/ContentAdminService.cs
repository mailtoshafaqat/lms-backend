using Lms.Modules.Content.Domain;
using Lms.Modules.Content.Infrastructure;
using Lms.Shared.Content;
using Lms.Shared.Events;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Content.Application;

public sealed class ContentAdminService : IContentAdminService
{
    private readonly ContentDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IEventBus _events;

    public ContentAdminService(ContentDbContext db, ITenantContext tenant, IEventBus events)
    {
        _db = db;
        _tenant = tenant;
        _events = events;
    }

    public async Task<LectureDto> AddLectureAsync(Guid topicId, CreateLectureRequest req, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(req.Url) ? null : req.Url.Trim();
        var storageKey = string.IsNullOrWhiteSpace(req.StorageKey) ? null : req.StorageKey.Trim();
        if (url is null && storageKey is null)
            throw new InvalidOperationException("Provide a video URL or upload a file.");

        var lecture = new Lecture
        {
            TenantId = _tenant.TenantId,
            TopicId = topicId,
            Title = req.Title.Trim(),
            Url = url,
            StorageKey = storageKey,
            DurationSec = req.DurationSec,
            Order = req.Order
        };
        _db.Lectures.Add(lecture);
        await _db.SaveChangesAsync(ct);
        var playUrl = lecture.Url ?? (lecture.StorageKey != null ? $"/api/v1/files/{lecture.StorageKey}" : null);
        return new LectureDto(lecture.Id, lecture.Title, playUrl, lecture.DurationSec, lecture.Order, false, false);
    }

    public async Task<NoteDto> AddNoteAsync(Guid topicId, CreateNoteRequest req, CancellationToken ct = default)
    {
        var contentHtml = string.IsNullOrWhiteSpace(req.ContentHtml) ? null : req.ContentHtml;
        var storageKey = string.IsNullOrWhiteSpace(req.StorageKey) ? null : req.StorageKey.Trim();
        if (contentHtml is null && storageKey is null)
            throw new InvalidOperationException("Add note text or upload a PDF/DOC file.");

        var note = new Note
        {
            TenantId = _tenant.TenantId,
            TopicId = topicId,
            Title = req.Title.Trim(),
            ContentHtml = contentHtml,
            StorageKey = storageKey,
            Order = req.Order
        };
        _db.Notes.Add(note);
        await _db.SaveChangesAsync(ct);
        await _events.PublishAsync(new NoteContentChangedEvent(_tenant.TenantId, topicId), ct);
        var downloadUrl = note.StorageKey != null ? $"/api/v1/files/{note.StorageKey}" : null;
        return new NoteDto(note.Id, note.Title, note.ContentHtml, downloadUrl, note.Order);
    }

    public async Task<bool> DeleteLectureAsync(Guid id, CancellationToken ct = default)
    {
        var lecture = await _db.Lectures.FindAsync([id], ct);
        if (lecture is null) return false;
        _db.Lectures.Remove(lecture);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteNoteAsync(Guid id, CancellationToken ct = default)
    {
        var note = await _db.Notes.FindAsync([id], ct);
        if (note is null) return false;
        var topicId = note.TopicId;
        _db.Notes.Remove(note);
        await _db.SaveChangesAsync(ct);
        await _events.PublishAsync(new NoteContentChangedEvent(_tenant.TenantId, topicId), ct);
        return true;
    }
}
