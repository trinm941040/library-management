using UTH.Library.Domain.Entities;

namespace UTH.Library.Domain.UnitTests;

public class AuditLogTests
{
    [Fact]
    public void Create_SetsRetentionWindow()
    {
        var createdAtUtc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

        var auditLog = AuditLog.Create(
            Guid.NewGuid(),
            "session.logged-in",
            "ApplicationUser",
            Guid.NewGuid(),
            null,
            null,
            createdAtUtc);

        Assert.Equal(createdAtUtc.AddYears(1), auditLog.RetainUntilUtc);
    }
}
