using Lms.Modules.SyllabusMentor.Application;
using Lms.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.SyllabusMentor.Api;

[ApiController]
[Route("api/v1/ai")]
public sealed class AiController : ControllerBase
{
    private readonly ISyllabusMentorService _mentor;
    private readonly ICurrentUser _currentUser;

    public AiController(ISyllabusMentorService mentor, ICurrentUser currentUser)
    {
        _mentor = mentor;
        _currentUser = currentUser;
    }

    [Authorize]
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();

        try
        {
            var response = await _mentor.AskAsync(
                userId.Value,
                _currentUser.Role ?? Roles.Student,
                request,
                ct);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
