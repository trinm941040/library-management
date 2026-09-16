using UTH.Library.Application.Abstractions.Persistence;

namespace UTH.Library.Application.Features.Dashboard;

public sealed class DashboardService(IDashboardRepository repository)
{
    public Task<DashboardSummaryModel> GetSummaryAsync(
        DashboardQuery query,
        Guid? userBranchId,
        bool hasAllBranchesAccess,
        CancellationToken cancellationToken)
    {
        return repository.GetSummaryAsync(query, userBranchId, hasAllBranchesAccess, cancellationToken);
    }

    public Task<IReadOnlyList<DashboardBranchItemModel>> GetBranchesAsync(CancellationToken cancellationToken)
    {
        return repository.GetBranchesAsync(cancellationToken);
    }
}
