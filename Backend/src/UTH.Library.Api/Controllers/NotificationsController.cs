using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Notifications;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Notifications;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController(
    INotificationService notificationService,
    ICurrentProfileService profileService) : ControllerBase
{
    [HttpPost("preview")]
    [ProducesResponseType(typeof(NotificationPreviewResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationPreviewResult>> Preview(
        [FromBody] NotificationPreviewApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new RenderNotificationPreviewQuery(
                request.TemplateCode,
                request.Variables ?? new Dictionary<string, string>());

            var result = await notificationService.PreviewAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpPost("send")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationDto>> Send(
        [FromBody] SendNotificationApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;
        var permissions = profile?.Permissions ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        if (!isAdmin && !permissions.Contains(Permissions.NotificationsManage))
        {
            return Forbid();
        }

        try
        {
            var cmd = new SendNotificationCommand(
                request.TemplateCode,
                request.RecipientType,
                request.RecipientId,
                request.Destination,
                request.Variables ?? new Dictionary<string, string>());

            var result = await notificationService.SendAsync(
                cmd,
                userId,
                HttpContext.TraceIdentifier,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return Ok(result);
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
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationDto>> Retry(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;
        var permissions = profile?.Permissions ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        if (!isAdmin && !permissions.Contains(Permissions.NotificationsManage))
        {
            return Forbid();
        }

        try
        {
            var result = await notificationService.RetryAsync(
                id,
                userId,
                HttpContext.TraceIdentifier,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(NotificationPageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NotificationPageResult>> GetHistory(
        [FromQuery] NotificationHistoryFilterRequest filter,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;
        var permissions = profile?.Permissions ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        if (!isAdmin && !permissions.Contains(Permissions.NotificationsRead) && !permissions.Contains(Permissions.NotificationsManage))
        {
            return Forbid();
        }

        var result = await notificationService.GetHistoryAsync(
            filter.Channel,
            filter.Status,
            filter.FromDate,
            filter.ToDate,
            filter.PageNumber,
            filter.PageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(NotificationPageResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPageResult>> GetMyNotifications(
        [FromQuery] UserNotificationFilterRequest filter,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await notificationService.GetMyNotificationsAsync(
            userId,
            filter.UnreadOnly,
            filter.PageNumber,
            filter.PageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(NotificationUnreadCountResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationUnreadCountResult>> GetUnreadCount(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;

        try
        {
            var updated = await notificationService.MarkReadAsync(id, userId, isAdmin, cancellationToken);
            if (!updated)
            {
                return NotFound(new ProblemDetails { Detail = $"Không tìm thấy thông báo ID: {id}" });
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        await notificationService.MarkAllReadAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("recipients")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationRecipientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<NotificationRecipientDto>>> SearchRecipients(
        [FromQuery] string type,
        [FromQuery] string? keyword,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;
        var permissions = profile?.Permissions ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        try
        {
            var recipients = await notificationService.SearchRecipientsAsync(
                type,
                keyword,
                permissions,
                isAdmin,
                cancellationToken);

            return Ok(recipients);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    private bool TryGetUserId(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
}
