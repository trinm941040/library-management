using Microsoft.EntityFrameworkCore;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Features.Reports;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Infrastructure.Persistence.Repositories;

public sealed class ReportRepository(
    LibraryDbContext dbContext,
    TimeProvider timeProvider) : IReportRepository
{
    public async Task<ReportPreviewResult> PreviewAsync(
        ReportDefinition definition,
        ReportPreviewRequest request,
        Guid? userBranchId,
        bool hasAllBranchesAccess,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var offsetMinutes = request.TimezoneOffsetMinutes ?? 0;
        var filters = request.Filters ?? new Dictionary<string, string?>();

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (rows, totalCount, summaryStats) = await ExecuteQueryAsync(
            definition.Code,
            filters,
            offsetMinutes,
            userBranchId,
            hasAllBranchesAccess,
            pageNumber,
            pageSize,
            isExport: false,
            cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new ReportPreviewResult(
            ReportCode: definition.Code,
            ReportName: definition.Name,
            Columns: definition.Columns,
            Rows: rows,
            TotalRows: totalCount,
            PageNumber: pageNumber,
            PageSize: pageSize,
            TotalPages: totalPages,
            SummaryStats: summaryStats,
            GeneratedAtUtc: nowUtc);
    }

    public async Task<(IReadOnlyList<Dictionary<string, object?>> Rows, int TotalRecords)> GetDataForExportAsync(
        ReportDefinition definition,
        ReportExportRequest request,
        Guid? userBranchId,
        bool hasAllBranchesAccess,
        CancellationToken cancellationToken)
    {
        var offsetMinutes = request.TimezoneOffsetMinutes ?? 0;
        var filters = request.Filters ?? new Dictionary<string, string?>();

        // Maximum 10,000 rows exported at once for performance
        var (rows, totalCount, _) = await ExecuteQueryAsync(
            definition.Code,
            filters,
            offsetMinutes,
            userBranchId,
            hasAllBranchesAccess,
            pageNumber: 1,
            pageSize: 10000,
            isExport: true,
            cancellationToken);

        return (rows, totalCount);
    }

    public async Task SaveReportAsync(Report report, CancellationToken cancellationToken)
    {
        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Report?> GetReportByIdAsync(Guid reportId, CancellationToken cancellationToken)
    {
        return dbContext.Reports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
    }

    private async Task<(IReadOnlyList<Dictionary<string, object?>> Rows, int TotalCount, Dictionary<string, object?>? SummaryStats)>
        ExecuteQueryAsync(
            string reportCode,
            Dictionary<string, string?> filters,
            int offsetMinutes,
            Guid? userBranchId,
            bool hasAllBranchesAccess,
            int pageNumber,
            int pageSize,
            bool isExport,
            CancellationToken cancellationToken)
    {
        return reportCode.ToLowerInvariant() switch
        {
            "circulation_loans" => await QueryCirculationLoansAsync(filters, offsetMinutes, userBranchId, hasAllBranchesAccess, pageNumber, pageSize, isExport, cancellationToken),
            "inventory_copies" => await QueryInventoryCopiesAsync(filters, userBranchId, hasAllBranchesAccess, pageNumber, pageSize, cancellationToken),
            "financial_fines" => await QueryFinancialFinesAsync(filters, offsetMinutes, pageNumber, pageSize, cancellationToken),
            "members_activity" => await QueryMembersActivityAsync(filters, pageNumber, pageSize, cancellationToken),
            _ => (new List<Dictionary<string, object?>>(), 0, null)
        };
    }

    private async Task<(IReadOnlyList<Dictionary<string, object?>> Rows, int TotalCount, Dictionary<string, object?>? SummaryStats)>
        QueryCirculationLoansAsync(
            Dictionary<string, string?> filters,
            int offsetMinutes,
            Guid? userBranchId,
            bool hasAllBranchesAccess,
            int pageNumber,
            int pageSize,
            bool isExport,
            CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Resolve branch scope
        Guid? branchId = null;
        if (!hasAllBranchesAccess && userBranchId.HasValue)
        {
            branchId = userBranchId.Value;
        }
        else if (filters.TryGetValue("branchId", out var bStr) && Guid.TryParse(bStr, out var bGuid))
        {
            branchId = bGuid;
        }

        List<Guid>? branchCopyIds = null;
        if (branchId.HasValue)
        {
            branchCopyIds = await (
                from area in dbContext.Areas.AsNoTracking()
                where area.BranchId == branchId.Value
                join shelf in dbContext.Shelves.AsNoTracking() on area.Id equals shelf.AreaId
                join copy in dbContext.BookCopies.AsNoTracking() on shelf.Id equals copy.ShelfId
                select copy.Id
            ).ToListAsync(cancellationToken);
        }

        var query = dbContext.Borrowings.AsNoTracking();

        if (branchCopyIds is not null)
        {
            query = query.Where(b => b.BookCopyId.HasValue && branchCopyIds.Contains(b.BookCopyId.Value));
        }

        // Filter by time range
        if (filters.TryGetValue("range", out var range) && !string.IsNullOrWhiteSpace(range) && range != "all")
        {
            var (startUtc, endUtc) = CalculateTimeRange(range, offsetMinutes, nowUtc);
            query = query.Where(b => b.BorrowedAtUtc >= startUtc && b.BorrowedAtUtc <= endUtc);
        }

        // Filter by status
        if (filters.TryGetValue("status", out var status) && !string.IsNullOrWhiteSpace(status) && status != "all")
        {
            query = status.ToLowerInvariant() switch
            {
                "borrowed" => query.Where(b => b.ReturnedAtUtc == null && b.DueAtUtc >= nowUtc),
                "overdue" => query.Where(b => b.ReturnedAtUtc == null && b.DueAtUtc < nowUtc),
                "returned" => query.Where(b => b.ReturnedAtUtc != null),
                _ => query
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Summary stats
        var activeCount = await query.CountAsync(b => b.ReturnedAtUtc == null, cancellationToken);
        var overdueCount = await query.CountAsync(b => b.ReturnedAtUtc == null && b.DueAtUtc < nowUtc, cancellationToken);
        var returnedCount = await query.CountAsync(b => b.ReturnedAtUtc != null, cancellationToken);

        var pagedQuery = query.OrderByDescending(b => b.BorrowedAtUtc);
        var rawItems = isExport
            ? await pagedQuery.Take(pageSize).ToListAsync(cancellationToken)
            : await pagedQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        // Enrich with book, copy, branch
        var bookIds = rawItems.Select(b => b.BookId).Distinct().ToList();
        var books = new Dictionary<Guid, string>();
        if (bookIds.Count > 0)
        {
            books = await dbContext.Books.AsNoTracking()
                .Where(bk => bookIds.Contains(bk.Id))
                .ToDictionaryAsync(bk => bk.Id, bk => bk.Title, cancellationToken);
        }

        var copyIds = rawItems.Where(b => b.BookCopyId.HasValue).Select(b => b.BookCopyId!.Value).Distinct().ToList();
        var copies = new Dictionary<Guid, (string Barcode, Guid? ShelfId)>();
        if (copyIds.Count > 0)
        {
            var copyList = await dbContext.BookCopies.AsNoTracking()
                .Where(c => copyIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Barcode, c.ShelfId })
                .ToListAsync(cancellationToken);
            foreach (var c in copyList)
            {
                copies[c.Id] = (c.Barcode, c.ShelfId);
            }
        }

        var shelfBranches = new Dictionary<Guid, string>();
        var shelfIds = copies.Values.Where(c => c.ShelfId.HasValue).Select(c => c.ShelfId!.Value).Distinct().ToList();
        if (shelfIds.Count > 0)
        {
            shelfBranches = await (
                from shelf in dbContext.Shelves.AsNoTracking()
                where shelfIds.Contains(shelf.Id)
                join area in dbContext.Areas.AsNoTracking() on shelf.AreaId equals area.Id
                join branch in dbContext.Branches.AsNoTracking() on area.BranchId equals branch.Id
                select new { ShelfId = shelf.Id, BranchName = branch.Name }
            ).ToDictionaryAsync(x => x.ShelfId, x => x.BranchName, cancellationToken);
        }

        var rows = new List<Dictionary<string, object?>>();
        foreach (var b in rawItems)
        {
            var copy = b.BookCopyId.HasValue && copies.TryGetValue(b.BookCopyId.Value, out var cVal) ? cVal : ((string Barcode, Guid? ShelfId)?)null;
            var branchName = copy?.ShelfId.HasValue == true
                ? shelfBranches.GetValueOrDefault(copy.Value.ShelfId!.Value, "Chính")
                : "Hệ thống";

            var st = b.ReturnedAtUtc != null ? "Đã trả" : (b.DueAtUtc < nowUtc ? "Quá hạn" : "Đang mượn");
            var bookTitle = books.TryGetValue(b.BookId, out var bk) ? bk : "Chưa xác định";

            rows.Add(new Dictionary<string, object?>
            {
                ["borrowingId"] = b.Id.ToString(),
                ["borrowerName"] = b.BorrowerName,
                ["borrowerEmail"] = b.BorrowerEmail,
                ["bookTitle"] = bookTitle,
                ["barcode"] = copy?.Barcode ?? string.Empty,
                ["borrowedAt"] = b.BorrowedAtUtc,
                ["dueAt"] = b.DueAtUtc,
                ["returnedAt"] = b.ReturnedAtUtc,
                ["status"] = st,
                ["branchName"] = branchName
            });
        }

        var summaryStats = new Dictionary<string, object?>
        {
            ["Tổng lượt mượn"] = totalCount,
            ["Đang mượn"] = activeCount,
            ["Quá hạn"] = overdueCount,
            ["Đã trả"] = returnedCount
        };

        return (rows, totalCount, summaryStats);
    }

    private async Task<(IReadOnlyList<Dictionary<string, object?>> Rows, int TotalCount, Dictionary<string, object?>? SummaryStats)>
        QueryInventoryCopiesAsync(
            Dictionary<string, string?> filters,
            Guid? userBranchId,
            bool hasAllBranchesAccess,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        Guid? branchId = null;
        if (!hasAllBranchesAccess && userBranchId.HasValue)
        {
            branchId = userBranchId.Value;
        }
        else if (filters.TryGetValue("branchId", out var bStr) && Guid.TryParse(bStr, out var bGuid))
        {
            branchId = bGuid;
        }

        var query = from copy in dbContext.BookCopies.AsNoTracking()
                    join book in dbContext.Books.AsNoTracking() on copy.BookId equals book.Id into bookGroup
                    from book in bookGroup.DefaultIfEmpty()
                    join shelf in dbContext.Shelves.AsNoTracking() on copy.ShelfId equals shelf.Id into shelfGroup
                    from shelf in shelfGroup.DefaultIfEmpty()
                    join area in dbContext.Areas.AsNoTracking() on shelf.AreaId equals area.Id into areaGroup
                    from area in areaGroup.DefaultIfEmpty()
                    join branch in dbContext.Branches.AsNoTracking() on area.BranchId equals branch.Id into branchGroup
                    from branch in branchGroup.DefaultIfEmpty()
                    select new
                    {
                        copy.Id,
                        copy.Barcode,
                        copy.Status,
                        copy.Condition,
                        copy.ShelfId,
                        BookTitle = book != null ? book.Title : "Chưa xác định",
                        Isbn = book != null ? book.Isbn : string.Empty,
                        BranchId = branch != null ? (Guid?)branch.Id : null,
                        BranchName = branch != null ? branch.Name : "Kho chung",
                        Location = shelf != null && area != null ? $"{area.Name} / {shelf.Code}" : "Chưa xếp kệ"
                    };

        if (branchId.HasValue)
        {
            query = query.Where(x => x.BranchId == branchId.Value);
        }

        if (filters.TryGetValue("status", out var stStr) && !string.IsNullOrWhiteSpace(stStr) && stStr != "all" &&
            Enum.TryParse<CopyStatus>(stStr, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (filters.TryGetValue("condition", out var condStr) && !string.IsNullOrWhiteSpace(condStr) && condStr != "all" &&
            Enum.TryParse<CopyCondition>(condStr, true, out var parsedCond))
        {
            query = query.Where(x => x.Condition == parsedCond);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var availableCount = await query.CountAsync(x => x.Status == CopyStatus.Available, cancellationToken);
        var borrowedCount = await query.CountAsync(x => x.Status == CopyStatus.Borrowed, cancellationToken);

        var pagedList = await query
            .OrderBy(x => x.Barcode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rows = pagedList.Select(x => new Dictionary<string, object?>
        {
            ["barcode"] = x.Barcode,
            ["bookTitle"] = x.BookTitle,
            ["isbn"] = x.Isbn,
            ["branchName"] = x.BranchName,
            ["location"] = x.Location,
            ["status"] = x.Status.ToString(),
            ["condition"] = x.Condition.ToString()
        }).ToList();

        var summaryStats = new Dictionary<string, object?>
        {
            ["Tổng bản sao"] = totalCount,
            ["Sẵn sàng"] = availableCount,
            ["Đang mượn"] = borrowedCount
        };

        return (rows, totalCount, summaryStats);
    }

    private async Task<(IReadOnlyList<Dictionary<string, object?>> Rows, int TotalCount, Dictionary<string, object?>? SummaryStats)>
        QueryFinancialFinesAsync(
            Dictionary<string, string?> filters,
            int offsetMinutes,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var query = dbContext.Violations.AsNoTracking();

        if (filters.TryGetValue("range", out var range) && !string.IsNullOrWhiteSpace(range) && range != "all")
        {
            var (startUtc, endUtc) = CalculateTimeRange(range, offsetMinutes, nowUtc);
            query = query.Where(v => v.RecordedAtUtc >= startUtc && v.RecordedAtUtc <= endUtc);
        }

        if (filters.TryGetValue("type", out var type) && !string.IsNullOrWhiteSpace(type) && type != "all")
        {
            query = query.Where(v => v.Type == type);
        }

        if (filters.TryGetValue("status", out var st) && !string.IsNullOrWhiteSpace(st) && st != "all")
        {
            query = st switch
            {
                "open" => query.Where(v => v.ResolvedAtUtc == null),
                "resolved" => query.Where(v => v.ResolvedAtUtc != null),
                _ => query
            };
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalFine = await query.SumAsync(v => (decimal?)v.FineAmount, cancellationToken) ?? 0m;

        var pagedItems = await query
            .OrderByDescending(v => v.RecordedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var violationIds = pagedItems.Select(v => v.Id).ToList();

        var adjustmentsMap = new Dictionary<Guid, decimal>();
        var paymentsMap = new Dictionary<Guid, decimal>();

        if (violationIds.Count > 0)
        {
            adjustmentsMap = await dbContext.FineAdjustments.AsNoTracking()
                .Where(a => violationIds.Contains(a.ViolationId))
                .GroupBy(a => a.ViolationId)
                .Select(g => new { ViolationId = g.Key, Amount = g.Sum(x => x.AmountDelta) })
                .ToDictionaryAsync(x => x.ViolationId, x => x.Amount, cancellationToken);

            paymentsMap = await dbContext.FinePayments.AsNoTracking()
                .Where(p => violationIds.Contains(p.ViolationId))
                .GroupBy(p => p.ViolationId)
                .Select(g => new { ViolationId = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(x => x.ViolationId, x => x.Amount, cancellationToken);
        }

        decimal totalOutstanding = 0m;
        var rows = new List<Dictionary<string, object?>>();

        foreach (var v in pagedItems)
        {
            var adj = adjustmentsMap.GetValueOrDefault(v.Id, 0m);
            var paid = paymentsMap.GetValueOrDefault(v.Id, 0m);
            var balance = Math.Max(0m, v.FineAmount + adj - paid);
            totalOutstanding += balance;

            var stLabel = v.ResolvedAtUtc != null ? "Đã giải quyết" : (balance == 0m ? "Đã thanh toán" : "Chưa thanh toán");

            rows.Add(new Dictionary<string, object?>
            {
                ["borrowerName"] = v.BorrowerName,
                ["borrowerEmail"] = v.BorrowerEmail,
                ["bookTitle"] = v.BookTitle,
                ["type"] = v.Type,
                ["fineAmount"] = v.FineAmount,
                ["adjustedAmount"] = adj,
                ["paidAmount"] = paid,
                ["balance"] = balance,
                ["recordedAt"] = v.RecordedAtUtc,
                ["status"] = stLabel
            });
        }

        var summaryStats = new Dictionary<string, object?>
        {
            ["Tổng số vi phạm"] = totalCount,
            ["Tổng tiền phạt"] = totalFine,
            ["Số dư nợ trang này"] = totalOutstanding
        };

        return (rows, totalCount, summaryStats);
    }

    private async Task<(IReadOnlyList<Dictionary<string, object?>> Rows, int TotalCount, Dictionary<string, object?>? SummaryStats)>
        QueryMembersActivityAsync(
            Dictionary<string, string?> filters,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
    {
        var query = dbContext.Members.AsNoTracking();

        if (filters.TryGetValue("status", out var st) && !string.IsNullOrWhiteSpace(st) && st != "all" &&
            Enum.TryParse<MemberStatus>(st, true, out var parsedStatus))
        {
            query = query.Where(m => m.Status == parsedStatus);
        }

        if (filters.TryGetValue("group", out var grp) && !string.IsNullOrWhiteSpace(grp) && grp != "all")
        {
            query = query.Where(m => m.MemberGroup == grp);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var activeCount = await query.CountAsync(m => m.Status == MemberStatus.Active, cancellationToken);

        var pagedMembers = await query
            .OrderBy(m => m.MemberCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var memberIds = pagedMembers.Select(m => m.Id).ToList();
        var activeBorrowingsMap = new Dictionary<Guid, int>();
        if (memberIds.Count > 0)
        {
            activeBorrowingsMap = await dbContext.Borrowings.AsNoTracking()
                .Where(b => memberIds.Contains(b.BorrowerId) && b.ReturnedAtUtc == null)
                .GroupBy(b => b.BorrowerId)
                .Select(g => new { BorrowerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BorrowerId, x => x.Count, cancellationToken);
        }

        var rows = pagedMembers.Select(m => new Dictionary<string, object?>
        {
            ["memberCode"] = m.MemberCode,
            ["fullName"] = m.FullName,
            ["email"] = m.Email,
            ["memberGroup"] = m.MemberGroup,
            ["status"] = m.Status.ToString(),
            ["borrowingLimit"] = m.BorrowingLimit,
            ["activeLoans"] = activeBorrowingsMap.GetValueOrDefault(m.Id, 0),
            ["createdAt"] = m.CreatedAtUtc
        }).ToList();

        var summaryStats = new Dictionary<string, object?>
        {
            ["Tổng độc giả"] = totalCount,
            ["Đang hoạt động"] = activeCount
        };

        return (rows, totalCount, summaryStats);
    }

    private static (DateTime StartUtc, DateTime EndUtc) CalculateTimeRange(string range, int offsetMinutes, DateTime nowUtc)
    {
        var localNow = nowUtc.AddMinutes(-offsetMinutes);
        DateTime periodStartUtc;
        DateTime periodEndUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

        switch (range.ToLowerInvariant())
        {
            case "today":
                var startOfToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Utc);
                periodStartUtc = DateTime.SpecifyKind(startOfToday.AddMinutes(offsetMinutes), DateTimeKind.Utc);
                break;
            case "30d":
                periodStartUtc = DateTime.SpecifyKind(nowUtc.AddDays(-30), DateTimeKind.Utc);
                break;
            case "month":
                var startOfMonth = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                periodStartUtc = DateTime.SpecifyKind(startOfMonth.AddMinutes(offsetMinutes), DateTimeKind.Utc);
                break;
            case "7d":
            default:
                periodStartUtc = DateTime.SpecifyKind(nowUtc.AddDays(-7), DateTimeKind.Utc);
                break;
        }

        return (periodStartUtc, periodEndUtc);
    }
}
