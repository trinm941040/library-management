using System.Globalization;
using System.Text;
using System.Text.Json;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;
using UTH.Library.Domain.Enums;

namespace UTH.Library.Application.Features.Reports;

public sealed class ReportService(
    IReportRepository reportRepository,
    TimeProvider timeProvider)
{
    private static readonly Dictionary<string, ReportDefinition> Definitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["circulation_loans"] = new(
            Code: "circulation_loans",
            Name: "Báo cáo khoản mượn lưu thông",
            Description: "Thống kê chi tiết các lượt mượn, hạn trả, tình trạng trả và quá hạn theo chi nhánh và khoảng thời gian.",
            Type: ReportType.Circulation,
            RequiredPermission: Permissions.BorrowingsRead,
            Columns:
            [
                new("borrowingId", "Mã lượt mượn", "string"),
                new("borrowerName", "Độc giả", "string"),
                new("borrowerEmail", "Email", "string"),
                new("bookTitle", "Tựa sách", "string"),
                new("barcode", "Mã vạch", "string"),
                new("borrowedAt", "Ngày mượn", "datetime"),
                new("dueAt", "Hạn trả", "datetime"),
                new("returnedAt", "Ngày trả", "datetime"),
                new("status", "Trạng thái", "badge"),
                new("branchName", "Chi nhánh", "string")
            ],
            FilterFields:
            [
                new("range", "Khoảng thời gian", "select",
                [
                    new("today", "Hôm nay"),
                    new("7d", "7 ngày qua"),
                    new("30d", "30 ngày qua"),
                    new("month", "Tháng này"),
                    new("all", "Tất cả")
                ]),
                new("branchId", "Chi nhánh", "branch"),
                new("status", "Trạng thái", "select",
                [
                    new("all", "Tất cả trạng thái"),
                    new("borrowed", "Đang mượn"),
                    new("overdue", "Quá hạn"),
                    new("returned", "Đã trả")
                ])
            ]),

        ["inventory_copies"] = new(
            Code: "inventory_copies",
            Name: "Báo cáo kho sách & bản sao",
            Description: "Tổng hợp tình trạng các bản sao sách trong kho, vị trí kệ, tình trạng hỏng/mất theo chi nhánh.",
            Type: ReportType.Inventory,
            RequiredPermission: Permissions.BooksRead,
            Columns:
            [
                new("barcode", "Mã vạch", "string"),
                new("bookTitle", "Tựa sách", "string"),
                new("isbn", "ISBN", "string"),
                new("branchName", "Chi nhánh", "string"),
                new("location", "Vị trí kệ", "string"),
                new("status", "Trạng thái", "badge"),
                new("condition", "Tình trạng", "string")
            ],
            FilterFields:
            [
                new("branchId", "Chi nhánh", "branch"),
                new("status", "Trạng thái", "select",
                [
                    new("all", "Tất cả trạng thái"),
                    new("Available", "Sẵn sàng"),
                    new("Borrowed", "Đang mượn"),
                    new("Reserved", "Đã đặt trước"),
                    new("Damaged", "Hư hỏng"),
                    new("Lost", "Mất sách")
                ]),
                new("condition", "Tình trạng", "select",
                [
                    new("all", "Tất cả tình trạng"),
                    new("New", "Mới"),
                    new("Good", "Tốt"),
                    new("Damaged", "Hư hỏng"),
                    new("Lost", "Mất")
                ])
            ]),

        ["financial_fines"] = new(
            Code: "financial_fines",
            Name: "Báo cáo vi phạm & tiền phạt",
            Description: "Thống kê các khoản phạt vi phạm mượn trả, hư hại hoặc làm mất sách, số tiền đã thu và số dư nợ.",
            Type: ReportType.Financial,
            RequiredPermission: Permissions.ViolationsRead,
            Columns:
            [
                new("borrowerName", "Độc giả", "string"),
                new("borrowerEmail", "Email", "string"),
                new("bookTitle", "Tựa sách", "string"),
                new("type", "Loại vi phạm", "badge"),
                new("fineAmount", "Tiền phạt gốc", "currency"),
                new("adjustedAmount", "Điều chỉnh/Miễn", "currency"),
                new("paidAmount", "Đã thanh toán", "currency"),
                new("balance", "Số dư nợ", "currency"),
                new("recordedAt", "Ngày ghi nhận", "datetime"),
                new("status", "Trạng thái", "badge")
            ],
            FilterFields:
            [
                new("range", "Khoảng thời gian", "select",
                [
                    new("today", "Hôm nay"),
                    new("7d", "7 ngày qua"),
                    new("30d", "30 ngày qua"),
                    new("month", "Tháng này"),
                    new("all", "Tất cả")
                ]),
                new("type", "Loại vi phạm", "select",
                [
                    new("all", "Tất cả vi phạm"),
                    new("overdue", "Quá hạn trả sách"),
                    new("damage", "Hư hỏng sách"),
                    new("lost", "Làm mất sách"),
                    new("other", "Khác")
                ]),
                new("status", "Trạng thái", "select",
                [
                    new("all", "Tất cả"),
                    new("open", "Chưa giải quyết"),
                    new("resolved", "Đã giải quyết")
                ])
            ]),

        ["members_activity"] = new(
            Code: "members_activity",
            Name: "Báo cáo danh sách độc giả",
            Description: "Danh sách độc giả, nhóm thành viên, trạng thái thẻ và giới hạn mượn.",
            Type: ReportType.Operational,
            RequiredPermission: Permissions.MembersRead,
            Columns:
            [
                new("memberCode", "Mã độc giả", "string"),
                new("fullName", "Họ và tên", "string"),
                new("email", "Email", "string"),
                new("memberGroup", "Nhóm", "string"),
                new("status", "Trạng thái", "badge"),
                new("borrowingLimit", "Giới hạn mượn", "number"),
                new("activeLoans", "Đang mượn", "number"),
                new("createdAt", "Ngày tham gia", "datetime")
            ],
            FilterFields:
            [
                new("status", "Trạng thái", "select",
                [
                    new("all", "Tất cả trạng thái"),
                    new("Active", "Đang hoạt động"),
                    new("Expired", "Hết hạn"),
                    new("Suspended", "Tạm khóa"),
                    new("Discontinued", "Ngừng sử dụng")
                ]),
                new("group", "Nhóm độc giả", "select",
                [
                    new("all", "Tất cả nhóm"),
                    new("student", "Sinh viên"),
                    new("lecturer", "Giảng viên"),
                    new("researcher", "Nghiên cứu sinh"),
                    new("general", "Bạn đọc ngoài")
                ])
            ])
    };

    public IReadOnlyList<ReportDefinition> GetAvailableDefinitions(
        IReadOnlyCollection<string> permissions,
        bool isAdmin)
    {
        if (isAdmin)
        {
            return Definitions.Values.ToList();
        }

        return Definitions.Values
            .Where(d => permissions.Contains(d.RequiredPermission) || permissions.Contains("reports.read"))
            .ToList();
    }

    public ReportDefinition? GetDefinition(string reportCode)
    {
        return Definitions.GetValueOrDefault(reportCode);
    }

    public async Task<ReportPreviewResult> PreviewAsync(
        ReportPreviewRequest request,
        Guid? userBranchId,
        bool hasAllBranchesAccess,
        IReadOnlyCollection<string> permissions,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var definition = GetDefinition(request.ReportCode)
            ?? throw new KeyNotFoundException($"Không tìm thấy loại báo cáo '{request.ReportCode}'.");

        if (!isAdmin && !permissions.Contains(definition.RequiredPermission) && !permissions.Contains("reports.read"))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem báo cáo này.");
        }

        return await reportRepository.PreviewAsync(
            definition,
            request,
            userBranchId,
            hasAllBranchesAccess,
            cancellationToken);
    }

    public async Task<ReportExportResult> ExportAsync(
        ReportExportRequest request,
        Guid userId,
        Guid? userBranchId,
        bool hasAllBranchesAccess,
        IReadOnlyCollection<string> permissions,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var definition = GetDefinition(request.ReportCode)
            ?? throw new KeyNotFoundException($"Không tìm thấy loại báo cáo '{request.ReportCode}'.");

        if (!isAdmin && !permissions.Contains(definition.RequiredPermission) && !permissions.Contains("reports.read"))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xuất báo cáo này.");
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var (rows, totalRecords) = await reportRepository.GetDataForExportAsync(
            definition,
            request,
            userBranchId,
            hasAllBranchesAccess,
            cancellationToken);

        // Generate CSV content with UTF-8 BOM
        var csvContent = GenerateCsv(definition.Columns, rows);
        var fileBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvContent)).ToArray();

        var fileId = Guid.NewGuid();
        var exportDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "Exports");
        Directory.CreateDirectory(exportDirectory);
        var filePath = Path.Combine(exportDirectory, $"{fileId}.csv");
        await File.WriteAllBytesAsync(filePath, fileBytes, cancellationToken);

        var expiresAtUtc = nowUtc.AddHours(24);
        var fileName = $"{definition.Code}_{nowUtc:yyyyMMdd_HHmmss}.csv";

        var metadata = new
        {
            FileId = fileId,
            FileName = fileName,
            FilePath = filePath,
            RecordCount = totalRecords,
            Filters = request.Filters,
            ExpiresAtUtc = expiresAtUtc
        };

        var reportEntity = Report.Create(
            code: $"{definition.Code}_{nowUtc:yyyyMMddHHmmss}",
            name: $"{definition.Name} - {nowUtc:dd/MM/yyyy HH:mm}",
            type: definition.Type,
            definition: JsonSerializer.Serialize(metadata),
            createdByUserId: userId,
            now: nowUtc);

        await reportRepository.SaveReportAsync(reportEntity, cancellationToken);

        return new ReportExportResult(
            ReportId: reportEntity.Id,
            ReportCode: definition.Code,
            FileName: fileName,
            ContentType: "text/csv; charset=utf-8",
            TotalRecords: totalRecords,
            ExpiresAtUtc: expiresAtUtc,
            DownloadUrl: $"/api/v1/reports/{reportEntity.Id}/download");
    }

    public async Task<ReportDownloadResult> DownloadAsync(
        Guid reportId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy báo cáo đã tạo.");

        if (!isAdmin && report.CreatedByUserId != userId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền tải tệp báo cáo này.");
        }

        var metaDoc = JsonDocument.Parse(report.Definition);
        var root = metaDoc.RootElement;
        var filePath = root.GetProperty("FilePath").GetString()!;
        var fileName = root.GetProperty("FileName").GetString()!;
        var expiresAtUtc = root.GetProperty("ExpiresAtUtc").GetDateTime();

        if (timeProvider.GetUtcNow().UtcDateTime > expiresAtUtc)
        {
            throw new InvalidOperationException("Tệp báo cáo này đã hết hạn lưu trữ (vượt quá 24 giờ). Vui lòng tạo lại báo cáo mới.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Tệp báo cáo không tồn tại trên hệ thống máy chủ.");
        }

        var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return new ReportDownloadResult(fileName, "text/csv; charset=utf-8", fileBytes);
    }

    private static string GenerateCsv(
        IReadOnlyList<ReportColumnDefinition> columns,
        IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsv(c.Label))));

        // Data rows
        foreach (var row in rows)
        {
            var values = columns.Select(col =>
            {
                var val = row.GetValueOrDefault(col.Key);
                if (val is null) return string.Empty;

                return col.Type switch
                {
                    "currency" when val is decimal d => d.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫",
                    "currency" when decimal.TryParse(val.ToString(), out var parsed) => parsed.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫",
                    "datetime" when val is DateTime dt => dt.ToString("dd/MM/yyyy HH:mm"),
                    "datetime" when DateTime.TryParse(val.ToString(), out var dt2) => dt2.ToString("dd/MM/yyyy HH:mm"),
                    _ => val.ToString() ?? string.Empty
                };
            });

            sb.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";
        var escaped = field.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
