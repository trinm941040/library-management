using Microsoft.EntityFrameworkCore;
using Npgsql;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Application.Common;
using UTH.Library.Domain.Entities;

namespace UTH.Library.Infrastructure.Persistence;

internal sealed class UnitOfWork(LibraryDbContext db) : IUnitOfWork
{
    public void AddAuditLog(AuditLog auditLog) => db.AuditLogs.Add(auditLog);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { return await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException exception)
        { throw new OptimisticConcurrencyException("The resource was modified by another request.", exception); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new ResourceConflictException("A unique value already exists."); }
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null) return await operation(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }
}
