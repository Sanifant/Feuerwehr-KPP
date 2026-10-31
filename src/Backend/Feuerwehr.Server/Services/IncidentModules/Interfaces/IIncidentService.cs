using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using System.Text.Json;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.EntityFrameworkCore;

using De.Openelp.Feuerwehr.Server.Services;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules;

public interface IIncidentService
{
    Task<ServiceResult> GetActiveIncident(CancellationToken cancellationToken);
    Task<ServiceResult> GetArchive(int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateIncident(CreateIncidentRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> CloseIncident(Guid incidentId, CloseIncidentRequest request, CancellationToken cancellationToken);
}
