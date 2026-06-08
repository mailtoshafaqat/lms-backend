using Lms.Modules.LiveClasses.Application;
using Lms.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.LiveClasses.Api;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class LiveClassesController : ControllerBase
{
    private readonly ILiveClassService _classes;
    private readonly ICurrentUser _currentUser;

    public LiveClassesController(ILiveClassService classes, ICurrentUser currentUser)
    {
        _classes = classes;
        _currentUser = currentUser;
    }

    [HttpGet("me/live-classes")]
    public async Task<IActionResult> MyClasses(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _classes.GetForUserAsync(userId.Value, ct));
    }
}
