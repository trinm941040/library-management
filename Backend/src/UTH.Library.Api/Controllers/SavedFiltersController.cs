using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Reports;
using UTH.Library.Application.Features.Reports;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/saved-filters")]
public sealed class SavedFiltersController(SavedFilterService filterService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SavedFilterResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SavedFilterResponse>>> Get(
        [FromQuery] string scope,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var filters = await filterService.GetUserFiltersAsync(userId, scope ?? string.Empty, cancellationToken);
        var response = filters.Select(f => new SavedFilterResponse(
            f.Id,
            f.Name,
            f.Scope,
            f.Criteria,
            f.Sort,
            f.CreatedAtUtc)).ToList();

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SavedFilterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SavedFilterResponse>> Create(
        [FromBody] CreateSavedFilterRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var created = await filterService.CreateAsync(
            userId,
            new CreateSavedFilterCommand(request.Name, request.Scope, request.Criteria, request.Sort),
            cancellationToken);

        var response = new SavedFilterResponse(
            created.Id,
            created.Name,
            created.Scope,
            created.Criteria,
            created.Sort,
            created.CreatedAtUtc);

        return CreatedAtAction(nameof(Get), new { scope = created.Scope }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSavedFilterRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var updated = await filterService.UpdateAsync(
            id,
            userId,
            new UpdateSavedFilterCommand(request.Name, request.Criteria, request.Sort),
            cancellationToken);

        if (!updated)
        {
            return NotFound(new ProblemDetails { Detail = "Không tìm thấy bộ lọc hoặc bạn không có quyền sở hữu." });
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var deleted = await filterService.DeleteAsync(id, userId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new ProblemDetails { Detail = "Không tìm thấy bộ lọc hoặc bạn không có quyền sở hữu." });
        }

        return NoContent();
    }

    private bool TryGetUserId(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
}
