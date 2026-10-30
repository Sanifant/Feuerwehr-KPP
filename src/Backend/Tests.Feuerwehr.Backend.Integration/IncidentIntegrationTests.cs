using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Tests.Backend.Integration.Infrastructure;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration;

[Collection(IntegrationTestCollection.Name)]
/// <summary>
/// Executes incident Integration Tests.
/// </summary>
/// <param name="fixture">The fixture.</param>
public sealed class IncidentIntegrationTests(IntegrationTestFixture fixture)
{
    [Fact]
    /// <summary>
    /// Executes incident Lifecycle Create Active Close Archive Works End To End.
    /// </summary>
    public async Task IncidentLifecycle_CreateActiveCloseArchive_WorksEndToEnd()
    {
        var token = await fixture.LoginAsAdminAndGetAccessTokenAsync();
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateIncidentRequest
        {
            Name = "Container E2E Incident",
            IncidentNumber = "IT-001",
            Keyword = "B2",
            OriginLatitude = 51.5,
            OriginLongitude = 10.25,
            AlarmedAtUtc = DateTimeOffset.UtcNow,
            SessionId = "integration-session",
            CommandId = Guid.NewGuid().ToString(),
        };

        var createResponse = await fixture.Client.PostAsJsonAsync("/api/incidents", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<IncidentCreatedResponse>();
        Assert.NotNull(created);
        Assert.Equal(createRequest.Name, created.Incident.Name);
        Assert.False(string.IsNullOrWhiteSpace(created.Lease.LeaseToken));

        var activeResponse = await fixture.Client.GetAsync("/api/incidents/active");
        Assert.Equal(HttpStatusCode.OK, activeResponse.StatusCode);

        var active = await activeResponse.Content.ReadFromJsonAsync<IncidentSummaryDto>();
        Assert.NotNull(active);
        Assert.Equal(created.Incident.Id, active.Id);

        var closeResponse = await fixture.Client.PostAsJsonAsync($"/api/incidents/{created.Incident.Id}/close", new CloseIncidentRequest
        {
            SessionId = created.Lease.SessionId,
            LeaseToken = created.Lease.LeaseToken,
            CommandId = Guid.NewGuid().ToString(),
        });
        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        var closed = await closeResponse.Content.ReadFromJsonAsync<IncidentClosedResponse>();
        Assert.NotNull(closed);
        Assert.Equal(IncidentStatus.Closed, closed.Incident.Status);

        var activeAfterClose = await fixture.Client.GetAsync("/api/incidents/active");
        Assert.Equal(HttpStatusCode.NotFound, activeAfterClose.StatusCode);

        var archiveResponse = await fixture.Client.GetAsync("/api/incidents/archive?page=1&pageSize=25");
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        var archive = await archiveResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<IncidentSummaryDto>>();
        Assert.NotNull(archive);
        Assert.Contains(archive, item => item.Id == created.Incident.Id);
    }
}
