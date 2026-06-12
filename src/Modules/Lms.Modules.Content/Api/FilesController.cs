using Lms.Shared.Storage;
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

    private readonly IFileStorage _storage;

    public FilesController(IFileStorage storage) => _storage = storage;

    /// <summary>Streams a stored file. In production these would be signed/short-lived URLs.</summary>
    [HttpGet("files/{*key}")]
    public async Task<IActionResult> Download(string key, CancellationToken ct)
    {
        var stream = await _storage.OpenAsync(key, ct);
        if (stream is null) return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        var contentType = provider.TryGetContentType(key, out var ct2) ? ct2 : "application/octet-stream";
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

        var key = $"{safeFolder}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        await using var stream = file.OpenReadStream();
        var savedKey = await _storage.SaveAsync(key, stream, ct);
        return Ok(new { key = savedKey, url = $"/api/v1/files/{savedKey}" });
    }
}
