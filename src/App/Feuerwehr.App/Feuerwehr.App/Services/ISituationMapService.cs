using System;
using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.App.Services;

public interface ISituationMapService
{
    Task<IncidentSummaryDto?> GetActiveIncidentAsync(CancellationToken cancellationToken);
    Task<MapStateResponse> GetStateAsync(Guid incidentId, CancellationToken cancellationToken);
}

