namespace UTH.Library.Application.Features.Dashboard;

public sealed record DashboardQuery(
    string? TimeRange = "7d",
    Guid? BranchId = null,
    int? TimezoneOffsetMinutes = 0);

public sealed record DashboardKpiModel(
    int TotalBooks,
    int TotalCopies,
    int AvailableCopies,
    int ActiveBorrowings,
    int ReturnedInPeriod,
    int ActiveMembers,
    int ActiveReservations,
    decimal OutstandingFineBalance,
    decimal CollectedFineInPeriod);

public sealed record DashboardOverdueItemModel(
    Guid BorrowingId,
    string BorrowerName,
    string BorrowerEmail,
    string BookTitle,
    string? CopyBarcode,
    DateTime DueAtUtc,
    int OverdueDays);

public sealed record DashboardExpiringReservationItemModel(
    Guid ReservationId,
    string ReserverName,
    string ReserverEmail,
    string BookTitle,
    DateTime ExpiresAtUtc,
    int RemainingHours);

public sealed record DashboardDamagedOrLostItemModel(
    Guid BookCopyId,
    string Barcode,
    string BookTitle,
    string Condition,
    string Status);

public sealed record DashboardInventoryDiscrepancyItemModel(
    Guid AuditItemId,
    Guid AuditId,
    string BookTitle,
    string Barcode,
    string Result,
    DateTime? ScannedAtUtc);

public sealed record DashboardAlertsModel(
    int OverdueLoansCount,
    IReadOnlyList<DashboardOverdueItemModel> OverdueItems,
    int ExpiringReservationsCount,
    IReadOnlyList<DashboardExpiringReservationItemModel> ExpiringReservationItems,
    int DamagedOrLostCopiesCount,
    IReadOnlyList<DashboardDamagedOrLostItemModel> DamagedOrLostItems,
    int InventoryDiscrepanciesCount,
    IReadOnlyList<DashboardInventoryDiscrepancyItemModel> InventoryDiscrepancyItems);

public sealed record DashboardActivityItemModel(
    Guid Id,
    string Action,
    string EntityType,
    Guid? EntityId,
    string ActorName,
    DateTime TimestampUtc,
    string? Details);

public sealed record DashboardBranchItemModel(
    Guid Id,
    string Code,
    string Name);

public sealed record DashboardSummaryModel(
    DashboardKpiModel Kpis,
    DashboardAlertsModel Alerts,
    IReadOnlyList<DashboardActivityItemModel> RecentActivities,
    Guid? AppliedBranchId,
    string? AppliedBranchName,
    DateTime DataTimestampUtc);
