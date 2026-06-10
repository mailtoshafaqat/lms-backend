using Lms.Modules.Flashcards.Application;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Flashcards.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Teacher")]
public sealed class AdminFlashcardsController : ControllerBase
{
    private readonly IFlashcardAdminService _admin;
    private readonly ISubjectAccessService _access;
    private readonly ICurrentUser _current;

    public AdminFlashcardsController(
        IFlashcardAdminService admin, ISubjectAccessService access, ICurrentUser current)
    {
        _admin = admin;
        _access = access;
        _current = current;
    }

    [HttpGet("topics/{topicId:guid}/flashcards")]
    public async Task<IActionResult> GetDeck(Guid topicId, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        return Ok(await _admin.GetAdminDeckAsync(topicId, ct));
    }

    [HttpPost("topics/{topicId:guid}/cards")]
    public async Task<IActionResult> AddCard(Guid topicId, [FromBody] CreateFlashcardRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        return Ok(await _admin.AddCardAsync(topicId, req, ct));
    }

    [HttpDelete("cards/{id:guid}")]
    public async Task<IActionResult> DeleteCard(Guid id, CancellationToken ct) =>
        await _admin.DeleteCardAsync(id, ct) ? NoContent() : NotFound();

    [HttpPut("cards/{id:guid}")]
    public async Task<IActionResult> UpdateCard(Guid id, [FromBody] UpdateFlashcardRequest req, CancellationToken ct)
    {
        var card = await _admin.UpdateCardAsync(id, req, ct);
        return card is null ? NotFound() : Ok(card);
    }

    [HttpPut("topics/{topicId:guid}/flashcards/title")]
    public async Task<IActionResult> UpdateDeckTitle(Guid topicId, [FromBody] UpdateDeckTitleRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        return await _admin.UpdateDeckTitleAsync(topicId, req, ct) ? Ok(new { updated = true }) : NotFound();
    }

    [HttpPut("topics/{topicId:guid}/flashcards/reorder")]
    public async Task<IActionResult> ReorderCards(Guid topicId, [FromBody] ReorderCardsRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        return await _admin.ReorderCardsAsync(topicId, req, ct) ? Ok(new { reordered = true }) : NotFound();
    }

    private Task<bool> CanManageTopic(Guid topicId, CancellationToken ct) =>
        _access.CanManageTopicAsync(_current.UserId ?? Guid.Empty, _current.Role ?? Roles.Student, topicId, ct);
}
