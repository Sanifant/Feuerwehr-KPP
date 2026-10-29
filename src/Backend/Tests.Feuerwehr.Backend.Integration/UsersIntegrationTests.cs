using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Tests.Backend.Integration.Infrastructure;

namespace de.openelp.feuerwehr.Tests.Backend.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class UsersIntegrationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task GetAllUsers_WithoutToken_ReturnsUnauthorized()
    {
        fixture.Client.DefaultRequestHeaders.Authorization = null;

        var response = await fixture.Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_WithAdminToken_ReturnsUsers()
    {
        var token = await fixture.LoginAsAdminAndGetAccessTokenAsync();
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await fixture.Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<UserSummaryDto>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
        Assert.Contains(users, user => string.Equals(user.Email, IntegrationTestFixture.SeedAdminEmail, StringComparison.OrdinalIgnoreCase));
    }
}