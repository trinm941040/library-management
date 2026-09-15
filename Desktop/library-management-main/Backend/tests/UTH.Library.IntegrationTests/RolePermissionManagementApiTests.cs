using System.Net;
using System.Net.Http.Json;
using UTH.Library.Api.Contracts.Permissions;
using UTH.Library.Api.Contracts.Roles;
using UTH.Library.Api.Contracts.Users;
using UTH.Library.Application.Abstractions.Identity;

namespace UTH.Library.IntegrationTests;

public sealed class RolePermissionManagementApiTests(UserManagementApiFactory factory)
    : IClassFixture<UserManagementApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task RoleAndPermissionCrud_ValidRequests_ManagesAssignmentAndDeletesResources()
    {
        var marker = Guid.NewGuid().ToString("N");
        var permissionName = $"catalog.read-{marker}";
        var permissionResponse = await client.PostAsJsonAsync(
            "/api/v1/permissions",
            new CreatePermissionRequest(permissionName, "Read a custom catalog", "catalog"));
        Assert.Equal(HttpStatusCode.Created, permissionResponse.StatusCode);
        var permission = await permissionResponse.Content.ReadFromJsonAsync<PermissionResponse>();
        Assert.NotNull(permission);

        var roleResponse = await client.PostAsJsonAsync(
            "/api/v1/roles",
            new CreateRoleRequest($"Librarian-{marker}", "Manages the catalog"));
        Assert.Equal(HttpStatusCode.Created, roleResponse.StatusCode);
        var role = await roleResponse.Content.ReadFromJsonAsync<RoleResponse>();
        Assert.NotNull(role);

        var assignResponse = await client.PutAsJsonAsync(
            $"/api/v1/roles/{role.Id}/permissions",
            new ReplaceRolePermissionsRequest([permission.Id]));
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        var assignedRole = await assignResponse.Content.ReadFromJsonAsync<RoleResponse>();
        Assert.NotNull(assignedRole);
        Assert.Equal(permission.Id, Assert.Single(assignedRole.Permissions).Id);

        var idempotentAssignResponse = await client.PutAsJsonAsync(
            $"/api/v1/roles/{role.Id}/permissions",
            new ReplaceRolePermissionsRequest([permission.Id]));
        Assert.Equal(HttpStatusCode.OK, idempotentAssignResponse.StatusCode);

        var updatedResponse = await client.PutAsJsonAsync(
            $"/api/v1/roles/{role.Id}",
            new UpdateRoleRequest($"Senior-Librarian-{marker}", "Updated description"));
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);

        var filteredPermissions = await client.GetFromJsonAsync<PermissionResponse[]>(
            $"/api/v1/permissions?search={marker}&module=catalog");
        Assert.NotNull(filteredPermissions);
        Assert.Equal(permission.Id, Assert.Single(filteredPermissions).Id);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/roles/{role.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/permissions/{permission.Id}")).StatusCode);
    }

    [Fact]
    public async Task ReplaceUserRoles_ValidRole_ReplacesDefaultRole()
    {
        var marker = Guid.NewGuid().ToString("N");
        var userResponse = await client.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserRequest($"roles-{marker}@example.com", "Strong-Password-123!", "Role Assignment User"));
        var user = await userResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);

        var roleResponse = await client.PostAsJsonAsync(
            "/api/v1/roles",
            new CreateRoleRequest($"Auditor-{marker}", "Audits library activity"));
        var role = await roleResponse.Content.ReadFromJsonAsync<RoleResponse>();
        Assert.NotNull(role);

        var assignResponse = await client.PutAsJsonAsync(
            $"/api/v1/users/{user.Id}/roles",
            new ReplaceUserRolesRequest([role.Id]));
        Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);

        var updatedUser = await client.GetFromJsonAsync<UserResponse>($"/api/v1/users/{user.Id}");
        Assert.NotNull(updatedUser);
        Assert.Equal(role.Name, Assert.Single(updatedUser.Roles));
    }

    [Fact]
    public async Task DeleteSystemRoleAndPermission_ReturnsConflict()
    {
        var roles = await client.GetFromJsonAsync<RoleResponse[]>("/api/v1/roles");
        Assert.NotNull(roles);
        var administrator = Assert.Single(roles, role => role.Name == RoleNames.Administrator);

        var permissions = await client.GetFromJsonAsync<PermissionResponse[]>("/api/v1/permissions");
        Assert.NotNull(permissions);
        var systemPermission = Assert.Single(permissions, permission => permission.Name == Permissions.RolesRead);

        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/roles/{administrator.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/v1/permissions/{systemPermission.Id}")).StatusCode);
    }

    [Fact]
    public async Task GetRoles_WithoutRolesReadPermission_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/roles");
        request.Headers.Add(TestAuthenticationHandler.PermissionsHeader, "none");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
