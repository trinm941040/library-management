namespace UTH.Library.Api.Contracts.Dashboard;

public sealed record DashboardSummaryFilterRequest(
    string? Range = "7d",
    Guid? BranchId = null,
    int? TimezoneOffsetMinutes = 0);

public sealed record DashboardSummaryResponse(
    KpiMetricsResponse Kpis,
    OperationalAlertsResponse Alerts,
    IReadOnlyList<DashboardActivityItemResponse> RecentActivities,
    IReadOnlyList<DashboardBranchItemResponse> AvailableBranches,
    Guid? SelectedBranchId,
    string? SelectedBranchName,
    string TimeRange,
    DateTime GeneratedAtUtc,
    int ClientTimezoneOffsetMinutes);

public sealed record KpiMetricsResponse(
    int TotalBooks,
    int TotalCopies,
    int AvailableCopies,
    int ActiveBorrowings,
    int ReturnedInPeriod,
    int ActiveMembers,
    int ActiveReservations,
    decimal OutstandingFineBalance,
    decimal CollectedFineInPeriod);

public sealed record OperationalAlertsResponse(
    int OverdueLoansCount,
    IReadOnlyList<OverdueAlertItemResponse> OverdueLoans,
    int ExpiringReservationsCount,
    IReadOnlyList<ExpiringReservationAlertItemResponse> ExpiringReservations,
    int DamagedOrLostCopiesCount,
    IReadOnlyList<DamagedOrLostCopyAlertItemResponse> DamagedOrLostCopies,
    int InventoryDiscrepanciesCount,
    IReadOnlyList<InventoryDiscrepancyAlertItemResponse> InventoryDiscrepancies,
    int TotalAlertCount);

public sealed record OverdueAlertItemResponse(
    Guid BorrowingId,
    string BorrowerName,
    string BorrowerEmail,
    string BookTitle,
    string? CopyBarcode,
    DateTime DueAtUtc,
    int OverdueDays);

public sealed record ExpiringReservationAlertItemResponse(
    Guid ReservationId,
    string ReserverName,
    string ReserverEmail,
    string BookTitle,
    DateTime ExpiresAtUtc,
    int RemainingHours);

public sealed record DamagedOrLostCopyAlertItemResponse(
    Guid BookCopyId,
    string Barcode,
    string BookTitle,
    string Condition,
    string Status);

public sealed record InventoryDiscrepancyAlertItemResponse(
    Guid AuditItemId,
    Guid AuditId,
    string BookTitle,
    string Barcode,
    string Result,
    DateTime? ScannedAtUtc);

public sealed record DashboardActivityItemResponse(
    Guid Id,
    string Action,
    string EntityType,
    Guid? EntityId,
    string ActorName,
    DateTime TimestampUtc,
    string? Details);

public sealed record DashboardBranchItemResponse(
    Guid Id,
    string Code,
    string Name);
