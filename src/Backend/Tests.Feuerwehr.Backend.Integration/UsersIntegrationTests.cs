using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Tests.Backend.Integration.Infrastructure;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration
{
    /// <summary>
    /// Tests users integration behavior and its expected outcomes.
    /// </summary>
    /// <param name="fixture">The shared test fixture providing application and database infrastructure.</param>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class UsersIntegrationTests(IntegrationTestFixture fixture)
    {
        /// <summary>
        /// Verifies that get all users without token returns unauthorized.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task GetAllUsers_WithoutToken_ReturnsUnauthorized()
        {
            fixture.Client.DefaultRequestHeaders.Authorization = null;

            HttpResponseMessage response = await fixture.Client.GetAsync("/api/users");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Verifies that get all users with admin token returns users.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task GetAllUsers_WithAdminToken_ReturnsUsers()
        {
            string token = await fixture.LoginAsAdminAndGetAccessTokenAsync();
            fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await fixture.Client.GetAsync("/api/users");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            IReadOnlyCollection<UserSummaryDto>? users = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<UserSummaryDto>>();
            Assert.NotNull(users);
            Assert.NotEmpty(users);
            Assert.Contains(users, user => string.Equals(user.Email, IntegrationTestFixture.SeedAdminEmail, StringComparison.OrdinalIgnoreCase));
        }
    }
}
