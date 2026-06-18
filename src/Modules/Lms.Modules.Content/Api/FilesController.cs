using Lms.Modules.Content.Application;
using Lms.Shared.Auth;
using Lms.Shared.Storage;
using Lms.Shared.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Lms.Modules.Content.Api;

[ApiController]
[Route("api/v1")]
public sealed class FilesController : ControllerBase
{
    private static readonly HashSet<string> BrandingImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp", "image/gif", "image/svg+xml"
    };

    private static readonly HashSet<string> LectureVideoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/webm", "video/quicktime", "application/octet-stream"
    };

    private static readonly HashSet<string> NoteDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/octet-stream"
    };

    private readonly IFileStorage _storage;
    private readonly ITenantStorageQuotaService _quota;
    private readonly ITenantContext _tenant;
    private readonly IStoredContentAccessService _access;
    private readonly ICurrentUser _currentUser;

    public FilesController(
        IFileStorage storage,
        ITenantStorageQuotaService quota,
        ITenantContext tenant,
        IStoredContentAccessService access,
        ICurrentUser currentUser)
    {
        _storage = storage;
        _quota = quota;
        _tenant = tenant;
        _access = access;
        _currentUser = currentUser;
    }

    /// <summary>Streams a stored file. Lecture/note keys require authentication.</summary>
    [HttpGet("files/{*key}")]
    public async Task<IActionResult> Download(string key, CancellationToken ct)
    {
        if (StoredFilePaths.RequiresAuthentication(key)
            && !(User.Identity?.IsAuthenticated ?? false))
            return Unauthorized();

        if (StoredFilePaths.RequiresAuthentication(key))
        {
            var allowed = await _access.CanDownloadAsync(_currentUser.UserId, _currentUser.Role, key, ct);
            if (!allowed)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "You are not enrolled in this course." });
        }

        var stream = await _storage.OpenAsync(key, ct);
        if (stream is null) return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        var contentType = provider.TryGetContentType(key, out var ct2) ? ct2 : "application/octet-stream";

        if (StoredFilePaths.IsLectureKey(key))
        {
            Response.Headers.ContentDisposition = "inline";
            return File(stream, contentType, enableRangeProcessing: true);
        }

        if (StoredFilePaths.IsNoteKey(key))
            return File(stream, contentType, Path.GetFileName(key));

        return File(stream, contentType, enableRangeProcessing: true);
    }

    /// <summary>Admin/teacher upload. Returns the storage key to attach to a lecture/note.</summary>
    [Authorize(Policy = "Teacher")]
    [HttpPost("admin/files")]
    [RequestSizeLimit(1_073_741_824)] // 1 GB
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string folder, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "No file uploaded." });

        var safeFolder = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder.Trim('/');
        if (safeFolder.Equals("branding", StringComparison.OrdinalIgnoreCase)
            || safeFolder.Equals("students", StringComparison.OrdinalIgnoreCase))
        {
            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new { error = "Image must be 2 MB or smaller." });
            if (!BrandingImageTypes.Contains(file.ContentType))
                return BadRequest(new { error = "Image must be PNG, JPEG, WebP, GIF, or SVG." });
        }
        else if (safeFolder.Equals("lectures", StringComparison.OrdinalIgnoreCase))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".mp4" or ".webm" or ".mov" or ".m4v"))
                return BadRequest(new { error = "Lecture video must be MP4, WebM, or MOV." });
            if (!string.IsNullOrWhiteSpace(file.ContentType)
                && !LectureVideoTypes.Contains(file.ContentType))
                return BadRequest(new { error = "Unsupported lecture video type." });
        }
        else if (safeFolder.Equals("notes", StringComparison.OrdinalIgnoreCase))
        {
            if (file.Length > 25 * 1024 * 1024)
                return BadRequest(new { error = "Note file must be 25 MB or smaller." });
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".pdf" or ".doc" or ".docx"))
                return BadRequest(new { error = "Notes must be PDF, DOC, or DOCX." });
            if (!string.IsNullOrWhiteSpace(file.ContentType)
                && !NoteDocumentTypes.Contains(file.ContentType))
                return BadRequest(new { error = "Unsupported note document type." });
        }

        var check = await _quota.CheckUploadAsync(_tenant.TenantId, file.Length, ct);
        if (!check.Allowed)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = check.Error, usage = check.Usage });

        var key = $"{safeFolder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        await using var stream = file.OpenReadStream();
        var savedKey = await _storage.SaveAsync(key, stream, ct);
        await _quota.RecordUploadAsync(_tenant.TenantId, savedKey, file.Length, safeFolder, ct);

        var usage = await _quota.GetUsageAsync(_tenant.TenantId, ct);
        return Ok(new { key = savedKey, url = $"/api/v1/files/{savedKey}", storage = usage });
    }
}
