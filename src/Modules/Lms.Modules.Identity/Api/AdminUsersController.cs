using Lms.Modules.Identity.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Identity.Api;

[ApiController]
[Route("api/v1/admin/students")]
[Authorize(Policy = "InstituteAdmin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _users;

    public AdminUsersController(IAdminUserService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _users.ListStudentsAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest req, CancellationToken ct)
    {
        var result = await _users.CreateStudentAsync(req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
