using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.Dashboard;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository(
    LibraryDbContext dbContext,
    IMemoryCache cache,
    TimeProvider timeProvider) : IDashboardRepository
{
    public async Task<IReadOnlyList<DashboardBranchItemModel>> GetBranchesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Branches
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new DashboardBranchItemModel(b.Id, b.Code, b.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<DashboardSummaryModel> GetSummaryAsync(
        DashboardQuery query,
        Guid? userBranchId,
        bool hasAllBranchesAccess,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Resolve branch filter
        Guid? targetBranchId = null;
        string? targetBranchName = null;

        if (!hasAllBranchesAccess && userBranchId.HasValue)
        {
            targetBranchId = userBranchId.Value;
        }
        else if (query.BranchId.HasValue && query.BranchId.Value != Guid.Empty)
        {
            targetBranchId = query.BranchId.Value;
        }

        if (targetBranchId.HasValue)
        {
            targetBranchName = await dbContext.Branches
                .AsNoTracking()
                .Where(b => b.Id == targetBranchId.Value)
                .Select(b => b.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var timeRange = string.IsNullOrWhiteSpace(query.TimeRange) ? "7d" : query.TimeRange.Trim().ToLowerInvariant();
        var offsetMinutes = query.TimezoneOffsetMinutes ?? 0;
        var cacheKey = $"dashboard_summary_{targetBranchId}_{timeRange}_{offsetMinutes}_{nowUtc:yyyyMMddHHmm}";

        if (cache.TryGetValue(cacheKey, out DashboardSummaryModel? cached) && cached is not null)
        {
            return cached;
        }

        // Calculate period window
        var localNow = nowUtc.AddMinutes(-offsetMinutes);
        DateTime periodStartUtc;
        DateTime periodEndUtc = nowUtc;

        switch (timeRange)
        {
            case "today":
                var startOfTodayLocal = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Unspecified);
                periodStartUtc = startOfTodayLocal.AddMinutes(offsetMinutes);
                break;
            case "30d":
                periodStartUtc = nowUtc.AddDays(-30);
                break;
            case "month":
                var startOfMonthLocal = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                periodStartUtc = startOfMonthLocal.AddMinutes(offsetMinutes);
                break;
            case "7d":
            default:
                periodStartUtc = nowUtc.AddDays(-7);
                break;
        }

        // Shelf IDs in target branch
        List<Guid>? shelfIdsInBranch = null;
        if (targetBranchId.HasValue)
        {
            shelfIdsInBranch = await (
                from area in dbContext.Areas.AsNoTracking()
                where area.BranchId == targetBranchId.Value
                join shelf in dbContext.Shelves.AsNoTracking() on area.Id equals shelf.AreaId
                select shelf.Id
            ).ToListAsync(cancellationToken);
        }

        // Copy IDs in branch
        List<Guid>? copyIdsInBranch = null;
        if (shelfIdsInBranch is not null)
        {
            copyIdsInBranch = await dbContext.BookCopies
                .AsNoTracking()
                .Where(c => c.ShelfId.HasValue && shelfIdsInBranch.Contains(c.ShelfId.Value))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
        }

        // 1. KPIs
        var copiesQuery = dbContext.BookCopies.AsNoTracking();
        if (copyIdsInBranch is not null)
        {
            copiesQuery = copiesQuery.Where(c => copyIdsInBranch.Contains(c.Id));
        }

        var totalCopies = await copiesQuery.CountAsync(cancellationToken);
        var availableCopies = await copiesQuery.CountAsync(c => c.Status == CopyStatus.Available, cancellationToken);

        var totalBooks = copyIdsInBranch is not null
            ? await copiesQuery.Select(c => c.BookId).Distinct().CountAsync(cancellationToken)
            : await dbContext.Books.AsNoTracking().CountAsync(cancellationToken);

        var borrowingsQuery = dbContext.Borrowings.AsNoTracking();
        if (copyIdsInBranch is not null)
        {
            borrowingsQuery = borrowingsQuery.Where(b => b.BookCopyId.HasValue && copyIdsInBranch.Contains(b.BookCopyId.Value));
        }

        var activeBorrowings = await borrowingsQuery.CountAsync(b => b.ReturnedAtUtc == null, cancellationToken);
        var returnedInPeriod = await borrowingsQuery.CountAsync(
            b => b.ReturnedAtUtc != null && b.ReturnedAtUtc >= periodStartUtc && b.ReturnedAtUtc <= periodEndUtc,
            cancellationToken);

        var activeMembers = await dbContext.Members.AsNoTracking().CountAsync(m => m.Status == MemberStatus.Active, cancellationToken);

        var reservationsQuery = dbContext.Reservations.AsNoTracking();
        var activeReservations = await reservationsQuery.CountAsync(r => r.FulfilledAtUtc == null && r.CancelledAtUtc == null, cancellationToken);

        // Fines & Payments
        var violationsQuery = dbContext.Violations.AsNoTracking();
        var openViolations = await violationsQuery
            .Where(v => v.ResolvedAtUtc == null)
            .Select(v => new { v.Id, v.FineAmount })
            .ToListAsync(cancellationToken);

        decimal outstandingFineBalance = 0m;
        if (openViolations.Count > 0)
        {
            var openViolationIds = openViolations.Select(v => v.Id).ToList();
            var adjustmentsMap = await dbContext.FineAdjustments
                .AsNoTracking()
                .Where(a => openViolationIds.Contains(a.ViolationId))
                .GroupBy(a => a.ViolationId)
                .Select(g => new { ViolationId = g.Key, TotalAdjusted = g.Sum(x => x.AmountDelta) })
                .ToDictionaryAsync(x => x.ViolationId, x => x.TotalAdjusted, cancellationToken);

            var paymentsMap = await dbContext.FinePayments
                .AsNoTracking()
                .Where(p => openViolationIds.Contains(p.ViolationId))
                .GroupBy(p => p.ViolationId)
                .Select(g => new { ViolationId = g.Key, TotalPaid = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(x => x.ViolationId, x => x.TotalPaid, cancellationToken);

            foreach (var v in openViolations)
            {
                var adjusted = adjustmentsMap.GetValueOrDefault(v.Id, 0m);
                var paid = paymentsMap.GetValueOrDefault(v.Id, 0m);
                var bal = Math.Max(0m, v.FineAmount + adjusted - paid);
                outstandingFineBalance += bal;
            }
        }

        var collectedFineInPeriod = await dbContext.FinePayments
            .AsNoTracking()
            .Where(p => p.PaidAtUtc >= periodStartUtc && p.PaidAtUtc <= periodEndUtc)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var kpis = new DashboardKpiModel(
            totalBooks,
            totalCopies,
            availableCopies,
            activeBorrowings,
            returnedInPeriod,
            activeMembers,
            activeReservations,
            outstandingFineBalance,
            collectedFineInPeriod);

        // 2. Operational Alerts
        // A. Overdue loans
        var overdueQuery = borrowingsQuery.Where(b => b.ReturnedAtUtc == null && b.DueAtUtc < nowUtc);
        var overdueLoansCount = await overdueQuery.CountAsync(cancellationToken);

        var topOverdueList = await (
            from b in overdueQuery
            join book in dbContext.Books.AsNoTracking() on b.BookId equals book.Id into bookGroup
            from book in bookGroup.DefaultIfEmpty()
            join copy in dbContext.BookCopies.AsNoTracking() on b.BookCopyId equals copy.Id into copyGroup
            from copy in copyGroup.DefaultIfEmpty()
            orderby b.DueAtUtc ascending
            select new
            {
                b.Id,
                b.BorrowerName,
                b.BorrowerEmail,
                BookTitle = book != null ? book.Title : "Tài liệu thư viện",
                CopyBarcode = copy != null ? copy.Barcode : null,
                b.DueAtUtc
            }
        ).Take(5).ToListAsync(cancellationToken);

        var overdueItems = topOverdueList.Select(x => new DashboardOverdueItemModel(
            x.Id,
            x.BorrowerName,
            x.BorrowerEmail,
            x.BookTitle,
            x.CopyBarcode,
            x.DueAtUtc,
            (int)Math.Max(1, (nowUtc - x.DueAtUtc).TotalDays)
        )).ToList();

        // B. Expiring reservations (within next 48 hours or recently expired)
        var expiringThreshold = nowUtc.AddDays(2);
        var expiringResQuery = reservationsQuery
            .Where(r => r.FulfilledAtUtc == null && r.CancelledAtUtc == null && r.ExpiresAtUtc <= expiringThreshold);

        var expiringCount = await expiringResQuery.CountAsync(cancellationToken);

        var topExpiringList = await (
            from r in expiringResQuery
            join book in dbContext.Books.AsNoTracking() on r.BookId equals book.Id into bookGroup
            from book in bookGroup.DefaultIfEmpty()
            orderby r.ExpiresAtUtc ascending
            select new
            {
                r.Id,
                r.ReserverName,
                r.ReserverEmail,
                BookTitle = book != null ? book.Title : "Tài liệu",
                r.ExpiresAtUtc
            }
        ).Take(5).ToListAsync(cancellationToken);

        var expiringItems = topExpiringList.Select(x => new DashboardExpiringReservationItemModel(
            x.Id,
            x.ReserverName,
            x.ReserverEmail,
            x.BookTitle,
            x.ExpiresAtUtc,
            (int)Math.Max(0, (x.ExpiresAtUtc - nowUtc).TotalHours)
        )).ToList();

        // C. Damaged or lost copies
        var damagedOrLostQuery = copiesQuery.Where(c =>
            c.Status == CopyStatus.Lost ||
            c.Status == CopyStatus.Damaged ||
            c.Condition == CopyCondition.Lost ||
            c.Condition == CopyCondition.Damaged);

        var damagedOrLostCount = await damagedOrLostQuery.CountAsync(cancellationToken);

        var topDamagedOrLostList = await (
            from c in damagedOrLostQuery
            join book in dbContext.Books.AsNoTracking() on c.BookId equals book.Id into bookGroup
            from book in bookGroup.DefaultIfEmpty()
            orderby c.AcquiredAtUtc descending
            select new
            {
                c.Id,
                c.Barcode,
                BookTitle = book != null ? book.Title : "Bản sao",
                Condition = c.Condition.ToString(),
                Status = c.Status.ToString()
            }
        ).Take(5).ToListAsync(cancellationToken);

        var damagedOrLostItems = topDamagedOrLostList.Select(x => new DashboardDamagedOrLostItemModel(
            x.Id,
            x.Barcode,
            x.BookTitle,
            x.Condition,
            x.Status
        )).ToList();

        // D. Inventory discrepancies
        var auditItemsQuery = dbContext.InventoryAuditItems.AsNoTracking();
        if (targetBranchId.HasValue)
        {
            auditItemsQuery = auditItemsQuery.Where(item =>
                dbContext.InventoryAudits.Any(a => a.Id == item.InventoryAuditId && a.BranchId == targetBranchId.Value));
        }

        var discrepancyQuery = auditItemsQuery.Where(item =>
            item.Result == AuditItemResult.Missing ||
            item.Result == AuditItemResult.Misplaced ||
            item.Result == AuditItemResult.Damaged);

        var discrepancyCount = await discrepancyQuery.CountAsync(cancellationToken);

        var topDiscrepancyList = await (
            from item in discrepancyQuery
            join copy in dbContext.BookCopies.AsNoTracking() on item.BookCopyId equals copy.Id into copyGroup
            from copy in copyGroup.DefaultIfEmpty()
            join book in dbContext.Books.AsNoTracking() on copy.BookId equals book.Id into bookGroup
            from book in bookGroup.DefaultIfEmpty()
            orderby item.ScannedAtUtc descending
            select new
            {
                item.Id,
                item.InventoryAuditId,
                BookTitle = book != null ? book.Title : "Bản sao kiểm kê",
                Barcode = copy != null ? copy.Barcode : string.Empty,
                Result = item.Result.ToString(),
                item.ScannedAtUtc
            }
        ).Take(5).ToListAsync(cancellationToken);

        var discrepancyItems = topDiscrepancyList.Select(x => new DashboardInventoryDiscrepancyItemModel(
            x.Id,
            x.InventoryAuditId,
            x.BookTitle,
            x.Barcode,
            x.Result,
            x.ScannedAtUtc
        )).ToList();

        var alerts = new DashboardAlertsModel(
            overdueLoansCount,
            overdueItems,
            expiringCount,
            expiringItems,
            damagedOrLostCount,
            damagedOrLostItems,
            discrepancyCount,
            discrepancyItems);

        // 3. Recent Activities (from AuditLogs)
        var recentAuditLogs = await (
            from log in dbContext.AuditLogs.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on log.ActorUserId equals user.Id into userGroup
            from user in userGroup.DefaultIfEmpty()
            orderby log.CreatedAtUtc descending
            select new
            {
                log.Id,
                log.Action,
                log.EntityType,
                log.EntityId,
                ActorName = user != null ? user.DisplayName : "Hệ thống",
                log.CreatedAtUtc,
                Details = log.CorrelationId
            }
        ).Take(8).ToListAsync(cancellationToken);

        var recentActivities = recentAuditLogs.Select(x => new DashboardActivityItemModel(
            x.Id,
            x.Action,
            x.EntityType,
            x.EntityId,
            x.ActorName,
            x.CreatedAtUtc,
            x.Details
        )).ToList();

        var summary = new DashboardSummaryModel(
            kpis,
            alerts,
            recentActivities,
            targetBranchId,
            targetBranchName,
            nowUtc);

        cache.Set(cacheKey, summary, TimeSpan.FromSeconds(45));
        return summary;
    }
}
