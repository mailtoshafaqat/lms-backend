using Lms.Modules.SyllabusMentor.Application;

using Lms.Shared.Auth;

using Lms.Shared.Courses;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;



namespace Lms.Modules.SyllabusMentor.Api;



[ApiController]

[Route("api/v1/admin/ai")]

[Authorize(Policy = "Teacher")]

public sealed class AdminAiController : ControllerBase

{

    private readonly ISyllabusMentorService _mentor;

    private readonly ISubjectAccessService _access;

    private readonly ICurrentUser _current;



    public AdminAiController(

        ISyllabusMentorService mentor, ISubjectAccessService access, ICurrentUser current)

    {

        _mentor = mentor;

        _access = access;

        _current = current;

    }



    [HttpPost("ingest")]

    public async Task<IActionResult> Ingest([FromBody] IngestRequest request, CancellationToken ct)

    {

        var userId = _current.UserId ?? Guid.Empty;

        var role = _current.Role ?? Roles.Student;



        if (request.TopicId is { } topicId)

        {

            if (!await _access.CanManageTopicAsync(userId, role, topicId, ct))

                return Forbid();

        }

        else if (request.SubjectId is { } subjectId)

        {

            if (!await _access.CanManageSubjectAsync(userId, role, subjectId, ct))

                return Forbid();

        }



        try

        {

            return Ok(await _mentor.IngestAsync(request, ct));

        }

        catch (ArgumentException ex)

        {

            return BadRequest(new { error = ex.Message });

        }

    }

}


