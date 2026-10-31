using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Tests.Backend.Integration.Infrastructure;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration
{
    [Collection(IntegrationTestCollection.Name)]
    /// <summary>
    /// Executes users Integration Tests.
    /// </summary>
    /// <param name="fixture">The fixture.</param>
    public sealed class UsersIntegrationTests(IntegrationTestFixture fixture)
    {
        [Fact]
        /// <summary>
        /// Gets the all Users Without Token Returns Unauthorized.
        /// </summary>
        public async Task GetAllUsers_WithoutToken_ReturnsUnauthorized()
        {
            fixture.Client.DefaultRequestHeaders.Authorization = null;

            HttpResponseMessage response = await fixture.Client.GetAsync("/api/users");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        /// <summary>
        /// Gets the all Users With Admin Token Returns Users.
        /// </summary>
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
