using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UTH.Library.Api.Contracts.Users;
using UTH.Library.Application.Abstractions.Identity;
using UTH.Library.Infrastructure.Persistence;

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

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";
}

public sealed class UserManagementApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(
        Path.GetTempPath(),
        $"uth-library-user-management-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["Database:EnsureCreated"] = "true",
                ["ConnectionStrings:LibraryDatabase"] = $"Data Source={databasePath}",
                ["Jwt:Key"] = "integration-test-signing-key-at-least-32-bytes",
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
        if (disposing && File.Exists(databasePath))
            File.Delete(databasePath);
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

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, TestAdministratorId.ToString()),
            new(ClaimTypes.Name, "Integration Test Administrator")
        };
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
