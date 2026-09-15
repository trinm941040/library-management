using System.Net;
using System.Net.Http.Json;
using UTH.Library.Api.Contracts.Members;
using UTH.Library.Domain.Entities;
namespace UTH.Library.IntegrationTests;

public sealed class MemberManagementApiTests(UserManagementApiFactory factory) : IClassFixture<UserManagementApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task MemberProfileAndCard_ValidRequests_ReturnCompleteDetails()
    {
        var marker = Guid.NewGuid().ToString("N");
        var create = await client.PostAsJsonAsync("/api/v1/members", new SaveMemberRequest(
            $"RD-{marker[..8]}", $"Reader {marker}", $"reader-{marker}@example.com", "0901234567",
            new DateOnly(2000, 1, 1), "Ho Chi Minh City", "Sinh viên", MemberStatus.Active, 5, 14));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var member = await create.Content.ReadFromJsonAsync<MemberResponse>(); Assert.NotNull(member);

        var card = await client.PostAsJsonAsync($"/api/v1/members/{member.Id}/card", new IssueCardRequest(
            $"CARD-{marker[..8]}", new DateOnly(2026, 9, 8), new DateOnly(2027, 9, 8)));
        Assert.Equal(HttpStatusCode.OK, card.StatusCode);
        var detailed = await card.Content.ReadFromJsonAsync<MemberResponse>(); Assert.NotNull(detailed);
        Assert.Equal($"CARD-{marker[..8]}".ToUpperInvariant(), detailed.Card?.CardNumber);
        Assert.Empty(detailed.Restrictions);
    }

    [Fact]
    public async Task GetMembers_WithoutPermission_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/members");
        request.Headers.Add(TestAuthenticationHandler.PermissionsHeader, "none");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(request)).StatusCode);
    }
}
