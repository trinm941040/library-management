using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTH.Library.Api.Contracts.Dashboard;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Features.Dashboard;

namespace UTH.Library.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
public sealed class DashboardController(
    DashboardService dashboardService,
    ICurrentProfileService profileService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(
        [FromQuery] DashboardSummaryFilterRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(new ProblemDetails { Detail = "Người dùng chưa đăng nhập." });
        }

        var profile = await profileService.GetAsync(userId, cancellationToken);
        var isGlobal = profile is null ||
                       profile.Roles.Contains(RoleNames.Administrator) ||
                       profile.Branch is null;

        var userBranchId = profile?.Branch?.Id;
        var query = new DashboardQuery(
            TimeRange: request.Range ?? "7d",
            BranchId: request.BranchId,
            TimezoneOffsetMinutes: request.TimezoneOffsetMinutes ?? 0);

        var summary = await dashboardService.GetSummaryAsync(query, userBranchId, isGlobal, cancellationToken);
        var branches = await dashboardService.GetBranchesAsync(cancellationToken);

        var totalAlerts = summary.Alerts.OverdueLoansCount +
                          summary.Alerts.ExpiringReservationsCount +
                          summary.Alerts.DamagedOrLostCopiesCount +
                          summary.Alerts.InventoryDiscrepanciesCount;

        var response = new DashboardSummaryResponse(
            Kpis: new KpiMetricsResponse(
                TotalBooks: summary.Kpis.TotalBooks,
                TotalCopies: summary.Kpis.TotalCopies,
                AvailableCopies: summary.Kpis.AvailableCopies,
                ActiveBorrowings: summary.Kpis.ActiveBorrowings,
                ReturnedInPeriod: summary.Kpis.ReturnedInPeriod,
                ActiveMembers: summary.Kpis.ActiveMembers,
                ActiveReservations: summary.Kpis.ActiveReservations,
                OutstandingFineBalance: summary.Kpis.OutstandingFineBalance,
                CollectedFineInPeriod: summary.Kpis.CollectedFineInPeriod),
            Alerts: new OperationalAlertsResponse(
                OverdueLoansCount: summary.Alerts.OverdueLoansCount,
                OverdueLoans: summary.Alerts.OverdueItems.Select(x => new OverdueAlertItemResponse(
                    x.BorrowingId, x.BorrowerName, x.BorrowerEmail, x.BookTitle, x.CopyBarcode, x.DueAtUtc, x.OverdueDays)).ToArray(),
                ExpiringReservationsCount: summary.Alerts.ExpiringReservationsCount,
                ExpiringReservations: summary.Alerts.ExpiringReservationItems.Select(x => new ExpiringReservationAlertItemResponse(
                    x.ReservationId, x.ReserverName, x.ReserverEmail, x.BookTitle, x.ExpiresAtUtc, x.RemainingHours)).ToArray(),
                DamagedOrLostCopiesCount: summary.Alerts.DamagedOrLostCopiesCount,
                DamagedOrLostCopies: summary.Alerts.DamagedOrLostItems.Select(x => new DamagedOrLostCopyAlertItemResponse(
                    x.BookCopyId, x.Barcode, x.BookTitle, x.Condition, x.Status)).ToArray(),
                InventoryDiscrepanciesCount: summary.Alerts.InventoryDiscrepanciesCount,
                InventoryDiscrepancies: summary.Alerts.InventoryDiscrepancyItems.Select(x => new InventoryDiscrepancyAlertItemResponse(
                    x.AuditItemId, x.AuditId, x.BookTitle, x.Barcode, x.Result, x.ScannedAtUtc)).ToArray(),
                TotalAlertCount: totalAlerts),
            RecentActivities: summary.RecentActivities.Select(x => new DashboardActivityItemResponse(
                x.Id, x.Action, x.EntityType, x.EntityId, x.ActorName, x.TimestampUtc, x.Details)).ToArray(),
            AvailableBranches: branches.Select(x => new DashboardBranchItemResponse(x.Id, x.Code, x.Name)).ToArray(),
            SelectedBranchId: summary.AppliedBranchId,
            SelectedBranchName: summary.AppliedBranchName,
            TimeRange: request.Range ?? "7d",
            GeneratedAtUtc: summary.DataTimestampUtc,
            ClientTimezoneOffsetMinutes: request.TimezoneOffsetMinutes ?? 0);

        return Ok(response);
    }

    [HttpGet("branches")]
    [ProducesResponseType(typeof(IReadOnlyList<DashboardBranchItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DashboardBranchItemResponse>>> GetBranches(CancellationToken cancellationToken)
    {
        var branches = await dashboardService.GetBranchesAsync(cancellationToken);
        return Ok(branches.Select(x => new DashboardBranchItemResponse(x.Id, x.Code, x.Name)).ToArray());
    }

    private bool TryGetUserId(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out id);
}
