using System.Net;
using System.Net.Http.Json;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Tests.Backend.Integration.Infrastructure;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration
{
    /// <summary>
    /// Tests auth integration behavior and its expected outcomes.
    /// </summary>
    /// <param name="fixture">The shared test fixture providing application and database infrastructure.</param>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class AuthIntegrationTests(IntegrationTestFixture fixture)
    {
        /// <summary>
        /// Verifies that login with seeded admin returns access and refresh token.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
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
