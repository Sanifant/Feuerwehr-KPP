using System.Net;
using System.Net.Http.Json;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Tests.Backend.Integration.Infrastructure;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration
{
    [Collection(IntegrationTestCollection.Name)]
    /// <summary>
    /// Executes auth Integration Tests.
    /// </summary>
    /// <param name="fixture">The fixture.</param>
    public sealed class AuthIntegrationTests(IntegrationTestFixture fixture)
    {
        [Fact]
        /// <summary>
        /// Executes login With Seeded Admin Returns Access And Refresh Token.
        /// </summary>
        public async Task Login_WithSeededAdmin_ReturnsAccessAndRefreshToken()
        {
            HttpResponseMessage response = await fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = IntegrationTestFixture.SeedAdminEmail,
                Password = IntegrationTestFixture.SeedAdminPassword,
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            LoginResponse? payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(payload);
            Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
            Assert.Contains("Admin", payload.Roles);
        }
    }
}
