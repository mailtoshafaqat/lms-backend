using Lms.Modules.Content.Application;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Content.Api;

[ApiController]
[Route("api/v1")]
public sealed class ContentController : ControllerBase
{
    private readonly IContentService _content;

    public ContentController(IContentService content) => _content = content;

    [HttpGet("topics/{topicId:guid}/content")]
    public async Task<IActionResult> GetTopicContent(Guid topicId, CancellationToken ct) =>
        Ok(await _content.GetTopicContentAsync(topicId, ct));
}
