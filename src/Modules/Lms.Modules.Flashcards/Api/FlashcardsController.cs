using Lms.Modules.Flashcards.Application;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Flashcards.Api;

[ApiController]
[Route("api/v1")]
public sealed class FlashcardsController : ControllerBase
{
    private readonly IFlashcardService _flashcards;

    public FlashcardsController(IFlashcardService flashcards) => _flashcards = flashcards;

    [HttpGet("topics/{topicId:guid}/flashcards")]
    public async Task<IActionResult> GetByTopic(Guid topicId, CancellationToken ct)
    {
        return Ok(await _flashcards.GetByTopicAsync(topicId, ct));
    }
}
