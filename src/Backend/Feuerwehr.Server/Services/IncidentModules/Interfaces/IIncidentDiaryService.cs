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

public interface IIncidentDiaryService
{
    Task<ServiceResult> GetEntries(Guid incidentId, CancellationToken cancellationToken);
    Task<ServiceResult> GetCategories(CancellationToken cancellationToken);
    Task<ServiceResult> CreateEntry(Guid incidentId, CreateDiaryEntryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateEntry(Guid incidentId, Guid entryId, UpdateDiaryEntryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> CancelEntry(Guid incidentId, Guid entryId, CancelDiaryEntryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> ExportDiaryPdf(Guid incidentId, CancellationToken cancellationToken);
}
