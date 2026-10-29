using System.Net;
using System.Net.Http.Json;
using de.openelp.feuerwehr.Common.Models.Auth;
using de.openelp.feuerwehr.Tests.Backend.Integration.Infrastructure;

namespace de.openelp.feuerwehr.Tests.Backend.Integration;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthIntegrationTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsAccessAndRefreshToken()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = IntegrationTestFixture.SeedAdminEmail,
            Password = IntegrationTestFixture.SeedAdminPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.Contains("Admin", payload.Roles);
    }
}