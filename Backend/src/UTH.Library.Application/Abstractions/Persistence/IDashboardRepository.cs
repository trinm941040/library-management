using UTH.Library.Application.Features.Dashboard;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IDashboardRepository
{
    Task<DashboardSummaryModel> GetSummaryAsync(
        DashboardQuery query,
        Guid? userBranchId,
        bool hasAllBranchesAccess,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DashboardBranchItemModel>> GetBranchesAsync(CancellationToken cancellationToken);
}
