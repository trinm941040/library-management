using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using UTH.Library.Api.Contracts.Roles;
using UTH.Library.Api.Contracts.Users;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Infrastructure.Persistence;
using UTH.Library.Infrastructure.Identity;

namespace UTH.Library.IntegrationTests;

public sealed class UserManagementApiTests : IClassFixture<UserManagementApiFactory>
{
    private readonly UserManagementApiFactory factory;
    private readonly HttpClient client;

    public UserManagementApiTests(UserManagementApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUser_ValidRequest_CreatesArgon2UserInDefaultRole()
    {
        var email = UniqueEmail();

        var response = await client.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserRequest(email, "Strong-Password-123!", "New Reader"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Contains("User", user.Roles);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var passwordHash = db.Users.Single(value => value.Id == user.Id).PasswordHash;
        Assert.StartsWith("$argon2id$v=19$", passwordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ModelSeed_Administrator_HasValidPasswordRoleAndAllPermissions()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var administrator = await userManager.FindByEmailAsync("admin@example.com");

        Assert.NotNull(administrator);
        Assert.True(administrator.IsActive);
        Assert.True(administrator.EmailConfirmed);
        Assert.True(await userManager.CheckPasswordAsync(administrator, "Admin@123"));
        Assert.Contains(RoleNames.Administrator, await userManager.GetRolesAsync(administrator));

        var administratorPermissions = await db.RolePermissions
            .Where(assignment => assignment.Role.NormalizedName == "ADMINISTRATOR")
            .Select(assignment => assignment.Permission.Name)
            .ToListAsync();
        Assert.All(Permissions.All, permission => Assert.Contains(permission, administratorPermissions));
    }

    [Fact]
    public async Task AdministratorAccount_OtherUserCannotMutateButAdministratorCanUpdateSelf()
    {
        var page = await client.GetFromJsonAsync<UserPageResponse>(
            "/api/v1/users?search=admin@example.com&pageNumber=1&pageSize=10");
        Assert.NotNull(page);
        var administrator = Assert.Single(page.Items);
        Assert.True(administrator.IsProtected);
        Assert.Contains(RoleNames.Administrator, administrator.Roles);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/users/{administrator.Id}",
            new UpdateUserRequest("changed-admin@example.com", "Changed Administrator"));
        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);

        var deactivateResponse = await client.DeleteAsync($"/api/v1/users/{administrator.Id}");
        Assert.Equal(HttpStatusCode.Conflict, deactivateResponse.StatusCode);

        var roles = await client.GetFromJsonAsync<RoleResponse[]>("/api/v1/roles");
        Assert.NotNull(roles);
        var userRole = Assert.Single(roles, role => role.Name == RoleNames.User);
        var replaceRolesResponse = await client.PutAsJsonAsync(
            $"/api/v1/users/{administrator.Id}/roles",
            new ReplaceUserRolesRequest([userRole.Id]));
        Assert.Equal(HttpStatusCode.Conflict, replaceRolesResponse.StatusCode);

        using var selfUpdateRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/users/{administrator.Id}")
        {
            Content = JsonContent.Create(new UpdateUserRequest(
                administrator.Email,
                "Self Updated Administrator"))
        };
        selfUpdateRequest.Headers.Add(TestAuthenticationHandler.UserIdHeader, administrator.Id.ToString());
        selfUpdateRequest.Headers.Add(TestAuthenticationHandler.RolesHeader, RoleNames.Administrator);
        var selfUpdateResponse = await client.SendAsync(selfUpdateRequest);
        Assert.Equal(HttpStatusCode.OK, selfUpdateResponse.StatusCode);
        var selfUpdated = await selfUpdateResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(selfUpdated);
        Assert.Equal("Self Updated Administrator", selfUpdated.DisplayName);

        using var restoreRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/users/{administrator.Id}")
        {
            Content = JsonContent.Create(new UpdateUserRequest(
                administrator.Email,
                administrator.DisplayName))
        };
        restoreRequest.Headers.Add(TestAuthenticationHandler.UserIdHeader, administrator.Id.ToString());
        restoreRequest.Headers.Add(TestAuthenticationHandler.RolesHeader, RoleNames.Administrator);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(restoreRequest)).StatusCode);

        var preserved = await client.GetFromJsonAsync<UserResponse>($"/api/v1/users/{administrator.Id}");
        Assert.NotNull(preserved);
        Assert.Equal("admin@example.com", preserved.Email);
        Assert.Equal("System Administrator", preserved.DisplayName);
        Assert.True(preserved.IsActive);
        Assert.True(preserved.IsProtected);
        Assert.Contains(RoleNames.Administrator, preserved.Roles);
    }

    [Fact]
    public async Task GetUsers_FilterBySearchActiveAndRole_ReturnsMatchingUser()
    {
        var marker = Guid.NewGuid().ToString("N");
        var email = $"filter-{marker}@example.com";
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserRequest(email, "Strong-Password-123!", $"Reader {marker}"));
        createResponse.EnsureSuccessStatusCode();

        var page = await client.GetFromJsonAsync<UserPageResponse>(
            $"/api/v1/users?search={marker}&isActive=true&role=User&pageNumber=1&pageSize=10");

        Assert.NotNull(page);
        var user = Assert.Single(page.Items);
        Assert.Equal(email, user.Email);
        Assert.True(user.IsActive);
        Assert.Contains("User", user.Roles);
    }

    [Fact]
    public async Task UpdateAndDeleteUser_ExistingUser_UpdatesThenSoftDeletesAndRevokesSession()
    {
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserRequest(UniqueEmail(), "Strong-Password-123!", "Before Update"));
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(created);

        var updatedEmail = UniqueEmail();
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/users/{created.Id}",
            new UpdateUserRequest(updatedEmail, "After Update"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(updated);
        Assert.Equal(updatedEmail, updated.Email);
        Assert.Equal("After Update", updated.DisplayName);

        var deleteResponse = await client.DeleteAsync($"/api/v1/users/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/users/{created.Id}");
        var deactivated = await getResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(deactivated);
        Assert.False(deactivated.IsActive);
    }

    [Fact]
    public async Task GetUsers_MissingUsersReadPermission_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
        request.Headers.Add(TestAuthenticationHandler.PermissionsHeader, "none");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AdministratorWithoutPermission_BypassesPermissionCheck()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users");
        request.Headers.Add(TestAuthenticationHandler.PermissionsHeader, "none");
        request.Headers.Add(TestAuthenticationHandler.RolesHeader, RoleNames.Administrator);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";
}

public sealed class UserManagementApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("uth_library_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public UserManagementApiFactory()
    {
        database.StartAsync().GetAwaiter().GetResult();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:MigrateOnStartup"] = "true",
                ["ConnectionStrings:LibraryDatabase"] = database.GetConnectionString(),
                ["Jwt:Issuer"] = "UTH.Library.Tests",
                ["Jwt:Audience"] = "UTH.Library.Tests"
            });
        });

        builder.ConfigureServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            database.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "UserManagementTest";
    public const string PermissionsHeader = "X-Test-Permissions";
    public const string RolesHeader = "X-Test-Roles";
    public const string UserIdHeader = "X-Test-User-Id";
    private static readonly Guid TestAdministratorId = new("11111111-1111-1111-1111-111111111111");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var permissionsHeader = Request.Headers[PermissionsHeader].ToString();
        var permissions = permissionsHeader switch
        {
            "none" => [],
            "" => Permissions.All.ToArray(),
            _ => permissionsHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        };
        var roles = Request.Headers[RolesHeader]
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var userId = Guid.TryParse(Request.Headers[UserIdHeader].ToString(), out var requestedUserId)
            ? requestedUserId
            : TestAdministratorId;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "Integration Test Administrator")
        };
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
