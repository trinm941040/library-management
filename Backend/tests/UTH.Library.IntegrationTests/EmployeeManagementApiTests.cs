using System.Net;
using System.Net.Http.Json;
using UTH.Library.Api.Contracts.Employees;
using UTH.Library.Domain.Entities;

namespace UTH.Library.IntegrationTests;

public sealed class EmployeeManagementApiTests(UserManagementApiFactory factory)
    : IClassFixture<UserManagementApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task EmployeeCrud_ValidRequests_CreatesFiltersUpdatesAndDeletesEmployee()
    {
        var marker = Guid.NewGuid().ToString("N");
        var employeeCode = $"EMP-{marker[..8]}";
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/employees",
            new CreateEmployeeRequest(
                employeeCode,
                $"Employee {marker}",
                $"employee-{marker}@example.com",
                "+84901234567",
                new DateOnly(1995, 5, 10),
                "Ho Chi Minh City",
                "Librarian",
                "Circulation",
                new DateOnly(2024, 1, 15),
                EmploymentStatus.Active));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();
        Assert.NotNull(created);
        Assert.Equal(employeeCode.ToUpperInvariant(), created.EmployeeCode);
        Assert.Equal(EmploymentStatus.Active, created.Status);

        var page = await client.GetFromJsonAsync<EmployeePageResponse>(
            $"/api/v1/employees?search={marker}&department=Circulation&status=Active&pageNumber=1&pageSize=10");
        Assert.NotNull(page);
        Assert.Equal(created.Id, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.TotalCount);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/employees/{created.Id}",
            new UpdateEmployeeRequest(
                employeeCode,
                $"Updated Employee {marker}",
                $"updated-employee-{marker}@example.com",
                "+84907654321",
                new DateOnly(1995, 5, 10),
                "Thu Duc City",
                "Senior Librarian",
                "Reference",
                new DateOnly(2024, 1, 15),
                EmploymentStatus.OnLeave));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<EmployeeResponse>();
        Assert.NotNull(updated);
        Assert.Equal($"Updated Employee {marker}", updated.FullName);
        Assert.Equal("Reference", updated.Department);
        Assert.Equal(EmploymentStatus.OnLeave, updated.Status);

        var getResponse = await client.GetFromJsonAsync<EmployeeResponse>($"/api/v1/employees/{created.Id}");
        Assert.NotNull(getResponse);
        Assert.Equal(updated.Email, getResponse.Email);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/employees/{created.Id}")).StatusCode);
        var terminated = await client.GetFromJsonAsync<EmployeeResponse>($"/api/v1/employees/{created.Id}");
        Assert.NotNull(terminated);
        Assert.Equal(EmploymentStatus.Terminated, terminated.Status);
    }

    [Fact]
    public async Task CreateEmployee_DuplicateEmployeeCode_ReturnsConflict()
    {
        var marker = Guid.NewGuid().ToString("N");
        var employeeCode = $"EMP-{marker[..8]}";
        var request = new CreateEmployeeRequest(
            employeeCode,
            $"Employee {marker}",
            $"first-{marker}@example.com",
            null,
            null,
            null,
            "Librarian",
            "Circulation",
            new DateOnly(2025, 1, 1),
            EmploymentStatus.Active);
        (await client.PostAsJsonAsync("/api/v1/employees", request)).EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/v1/employees",
            request with { Email = $"second-{marker}@example.com" });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task GetEmployees_WithoutEmployeesReadPermission_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/employees");
        request.Headers.Add(TestAuthenticationHandler.PermissionsHeader, "none");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
