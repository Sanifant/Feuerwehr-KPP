using System;
using System.Threading;
using System.Threading.Tasks;
using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Common.Models.IncidentModules;

namespace de.openelp.feuerwehr.App.Services;

public interface ISituationMapService
{
    Task<IncidentSummaryDto?> GetActiveIncidentAsync(CancellationToken cancellationToken);
    Task<MapStateResponse> GetStateAsync(Guid incidentId, CancellationToken cancellationToken);
}
