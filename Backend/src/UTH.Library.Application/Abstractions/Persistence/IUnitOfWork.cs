using UTH.Library.Domain.Entities;

namespace UTH.Library.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    void AddAuditLog(AuditLog auditLog);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken);
}
