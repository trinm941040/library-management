using Microsoft.EntityFrameworkCore;
using UTH.Library.Infrastructure.Persistence;

namespace UTH.Library.Infrastructure.Identity;

internal static class SessionLock
{
    // Transaction-scoped lock shared by rotation, logout and password changes.
    // PostgreSQL releases it on commit/rollback; no process-local semaphore.
    public static Task AcquireAsync(LibraryDbContext db, Guid userId, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({userId.ToString()}, 0))", ct);
}
