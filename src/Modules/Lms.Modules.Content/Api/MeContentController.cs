using Lms.Modules.Content.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Content.Api;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeContentController : ControllerBase
{
    private readonly IVideoLibraryService _library;

    public MeContentController(IVideoLibraryService library) => _library = library;

    [HttpGet("video-library")]
    public async Task<IActionResult> GetVideoLibrary(CancellationToken ct) =>
        Ok(await _library.GetMyLibraryAsync(ct));
}
