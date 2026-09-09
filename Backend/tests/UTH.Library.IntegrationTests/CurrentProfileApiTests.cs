using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using UTH.Library.Api.Contracts.Profile;
using UTH.Library.Domain.Entities;
using UTH.Library.Infrastructure.Persistence;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.IntegrationTests;

public sealed class CurrentProfileApiTests(UserManagementApiFactory factory)
    : IClassFixture<UserManagementApiFactory>
{
    private static readonly Guid AdministratorId = new("10000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task GetAndPatchProfile_ReturnsCurrentUserAndEnforcesConcurrency()
    {
        await EnsureEmployeeAsync(factory);
        var client = factory.CreateClient();
        using var getRequest = Request(HttpMethod.Get, "/api/v1/me");
        var getResponse = await client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var profile = await getResponse.Content.ReadFromJsonAsync<CurrentProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal(AdministratorId, profile.UserId);
        Assert.Equal("MAIN", profile.Branch?.Code);
        Assert.Contains("employees.read", profile.Permissions);
        Assert.DoesNotContain("users.create", profile.Permissions);
        Assert.NotNull(profile.RowVersion);

        var updatedName = $"Profile {Guid.NewGuid():N}";
        using var patchRequest = Request(HttpMethod.Patch, "/api/v1/me/profile");
        patchRequest.Headers.Add("X-Correlation-ID", "profile-test");
        patchRequest.Content = JsonContent.Create(new UpdateCurrentProfileRequest(
            updatedName, "+84901234567", new DateOnly(1990, 1, 1), "Ho Chi Minh City", profile.RowVersion!.Value));
        var patchResponse = await client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var updated = await patchResponse.Content.ReadFromJsonAsync<CurrentProfileResponse>();
        Assert.NotNull(updated);
        Assert.Equal(updatedName, updated.FullName);
        Assert.Equal(updatedName, updated.DisplayName);
        Assert.NotEqual(profile.RowVersion, updated.RowVersion);

        using var staleRequest = Request(HttpMethod.Patch, "/api/v1/me/profile");
        staleRequest.Content = JsonContent.Create(new UpdateCurrentProfileRequest(
            "Stale update", null, null, null, profile.RowVersion.Value));
        Assert.Equal(HttpStatusCode.Conflict, (await client.SendAsync(staleRequest)).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var audit = await db.AuditLogs.OrderByDescending(value => value.CreatedAtUtc)
            .FirstAsync(value => value.ActorUserId == AdministratorId && value.Action == "profile.updated");
        Assert.NotNull(audit.CorrelationId);
        Assert.Null(audit.BeforeJson);
        Assert.Null(audit.AfterJson);
    }

    [Fact]
    public async Task ChangePassword_RevokesEveryRefreshSessionAndRedactsAudit()
    {
        var userId = Guid.NewGuid();
        const string currentPassword = "Current-Password-123!";
        const string newPassword = "New-Password-456!";
        using (var setupScope = factory.Services.CreateScope())
        {
            var userManager = setupScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = setupScope.ServiceProvider.GetRequiredService<LibraryDbContext>();
            var user = new ApplicationUser
            {
                Id = userId,
                UserName = $"profile-{userId:N}@example.com",
                Email = $"profile-{userId:N}@example.com",
                DisplayName = "Password Test",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            Assert.True((await userManager.CreateAsync(user, currentPassword)).Succeeded);
            db.RefreshTokenSessions.AddRange(
                Session(userId, "FIRST"),
                Session(userId, "SECOND"));
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/change-password")
        {
            Content = JsonContent.Create(new ChangePasswordRequest(currentPassword, newPassword))
        };
        request.Headers.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString());
        request.Headers.Add("X-Correlation-ID", "change-password-test");
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(request)).StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var updatedUser = await verifyManager.FindByIdAsync(userId.ToString());
        Assert.NotNull(updatedUser);
        Assert.True(await verifyManager.CheckPasswordAsync(updatedUser, newPassword));
        Assert.All(await verifyDb.RefreshTokenSessions.Where(value => value.UserId == userId).ToArrayAsync(), value => Assert.NotNull(value.RevokedAtUtc));
        var audit = await verifyDb.AuditLogs.SingleAsync(value => value.ActorUserId == userId && value.Action == "password.changed");
        Assert.Equal("change-password-test", audit.CorrelationId);
        Assert.Null(audit.BeforeJson);
        Assert.Null(audit.AfterJson);
    }

    private static HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(TestAuthenticationHandler.UserIdHeader, AdministratorId.ToString());
        request.Headers.Add(TestAuthenticationHandler.RolesHeader, "Administrator");
        request.Headers.Add(TestAuthenticationHandler.PermissionsHeader, "employees.read,users.read");
        return request;
    }

    private static async Task EnsureEmployeeAsync(UserManagementApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        if (await db.Employees.AnyAsync(value => value.UserId == AdministratorId)) return;
        var now = DateTime.UtcNow;
        var employee = Employee.Create(
            $"ADM-{Guid.NewGuid():N}"[..12], "System Administrator", $"profile-{Guid.NewGuid():N}@example.com",
            null, new DateOnly(1990, 1, 1), null, "Administrator", "System",
            new DateOnly(2020, 1, 1), EmploymentStatus.Active, now, Branch.MainBranchId);
        employee.LinkUser(AdministratorId, now);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
    }

    private static RefreshTokenSession Session(Guid userId, string tokenHash)
    {
        var now = DateTime.UtcNow;
        return new RefreshTokenSession
        {
            Id = Guid.NewGuid(), UserId = userId, TokenHash = $"{tokenHash}-{Guid.NewGuid():N}",
            FamilyId = Guid.NewGuid(), CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(7)
        };
    }
}
