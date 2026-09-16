using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Reports;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Reports;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reports")]
public sealed class ReportsController(
    ReportService reportService,
    ICurrentProfileService profileService) : ControllerBase
{
    [HttpGet("definitions")]
    [ProducesResponseType(typeof(IReadOnlyList<ReportDefinitionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReportDefinitionResponse>>> GetDefinitions(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;
        var permissions = profile?.Permissions ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        var list = reportService.GetAvailableDefinitions(permissions, isAdmin);
        var response = list.Select(d => new ReportDefinitionResponse(
            d.Code,
            d.Name,
            d.Description,
            d.Type.ToString(),
            d.Columns,
            d.FilterFields)).ToList();

        return Ok(response);
    }

    [HttpPost("preview")]
    [ProducesResponseType(typeof(ReportPreviewResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportPreviewResult>> Preview(
        [FromBody] ReportPreviewApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;
        var permissions = profile?.Permissions ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        var userBranchId = profile?.Branch?.Id;
        var hasAllBranchesAccess = isAdmin || profile?.Branch is null;

        try
        {
            var previewRequest = new ReportPreviewRequest(
                request.ReportCode,
                request.Filters,
                request.PageNumber,
                request.PageSize,
                request.SortField,
                request.SortAscending,
                request.TimezoneOffsetMinutes);

            var result = await reportService.PreviewAsync(
                previewRequest,
                userBranchId,
                hasAllBranchesAccess,
                permissions,
                isAdmin,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpPost("export")]
    [ProducesResponseType(typeof(ReportExportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportExportResult>> Export(
        [FromBody] ReportExportApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;
        var permissions = profile?.Permissions ?? (IReadOnlyCollection<string>)Array.Empty<string>();

        var userBranchId = profile?.Branch?.Id;
        var hasAllBranchesAccess = isAdmin || profile?.Branch is null;

        try
        {
            var exportRequest = new ReportExportRequest(
                request.ReportCode,
                request.Filters,
                request.Format,
                request.SortField,
                request.SortAscending,
                request.TimezoneOffsetMinutes);

            var result = await reportService.ExportAsync(
                exportRequest,
                userId,
                userBranchId,
                hasAllBranchesAccess,
                permissions,
                isAdmin,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isAdmin = profile?.Roles.Contains(RoleNames.Administrator) == true;

        try
        {
            var download = await reportService.DownloadAsync(id, userId, isAdmin, cancellationToken);
            return File(download.FileBytes, download.ContentType, download.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status410Gone, new ProblemDetails { Detail = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
    }

    private bool TryGetUserId(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
}
