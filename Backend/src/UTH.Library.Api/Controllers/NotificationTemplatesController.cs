using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Notifications;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Notifications;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notification-templates")]
public sealed class NotificationTemplatesController(
    INotificationService notificationService,
    ICurrentProfileService profileService) : ControllerBase
{
    [HttpGet("events")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationEventDefinition>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<NotificationEventDefinition>> GetEvents() =>
        Ok(notificationService.GetEventDefinitions());

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationTemplateDto>>> GetAll(CancellationToken cancellationToken)
    {
        var templates = await notificationService.GetTemplatesAsync(cancellationToken);
        return Ok(templates);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationTemplateDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var template = await notificationService.GetTemplateByIdAsync(id, cancellationToken);
        if (template is null)
        {
            return NotFound(new ProblemDetails { Detail = $"Không tìm thấy mẫu thông báo với ID: {id}" });
        }

        return Ok(template);
    }

    [HttpPost]
    [ProducesResponseType(typeof(NotificationTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationTemplateDto>> Create(
        [FromBody] CreateNotificationTemplateApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;
        var permissions = profile?.Permissions ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        if (!isAdmin && !permissions.Contains(Permissions.NotificationTemplatesManage) && !permissions.Contains(Permissions.NotificationsManage))
        {
            return Forbid();
        }

        try
        {
            var cmd = new CreateNotificationTemplateCommand(
                request.Code,
                request.Name,
                request.Channel,
                request.SubjectTemplate,
                request.BodyTemplate,
                request.AllowedVariables,
                request.IsActive);

            var created = await notificationService.CreateTemplateAsync(
                cmd,
                userId,
                HttpContext.TraceIdentifier,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NotificationTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationTemplateDto>> Update(
        Guid id,
        [FromBody] UpdateNotificationTemplateApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;
        var permissions = profile?.Permissions ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        if (!isAdmin && !permissions.Contains(Permissions.NotificationTemplatesManage) && !permissions.Contains(Permissions.NotificationsManage))
        {
            return Forbid();
        }

        try
        {
            var cmd = new UpdateNotificationTemplateCommand(
                request.Name,
                request.Channel,
                request.SubjectTemplate,
                request.BodyTemplate,
                request.AllowedVariables,
                request.IsActive,
                request.ConcurrencyToken);

            var updated = await notificationService.UpdateTemplateAsync(
                id,
                cmd,
                userId,
                HttpContext.TraceIdentifier,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;
        var permissions = profile?.Permissions ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        if (!isAdmin && !permissions.Contains(Permissions.NotificationTemplatesManage) && !permissions.Contains(Permissions.NotificationsManage))
        {
            return Forbid();
        }

        var deleted = await notificationService.DeleteTemplateAsync(
            id,
            userId,
            HttpContext.TraceIdentifier,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (!deleted)
        {
            return NotFound(new ProblemDetails { Detail = $"Không tìm thấy mẫu thông báo với ID: {id}" });
        }

        return NoContent();
    }

    private bool TryGetUserId(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
}
