using UTH.Library.Application.Features.Reports;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IReportRepository
{
    Task<ReportPreviewResult> PreviewAsync(
        ReportDefinition definition,
        ReportPreviewRequest request,
        Guid? userBranchId,
        bool hasAllBranchesAccess,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<Dictionary<string, object?>> Rows, int TotalRecords)> GetDataForExportAsync(
        ReportDefinition definition,
        ReportExportRequest request,
        Guid? userBranchId,
        bool hasAllBranchesAccess,
        CancellationToken cancellationToken);

    Task SaveReportAsync(Report report, CancellationToken cancellationToken);

    Task<Report?> GetReportByIdAsync(Guid reportId, CancellationToken cancellationToken);
}
