using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Services;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public interface ISituationMapService
    {
        Task<ServiceResult> GetState(Guid incidentId, CancellationToken cancellationToken);
        Task<ServiceResult> AcquireLease(Guid incidentId, AcquireLeaseRequest request, CancellationToken cancellationToken);
        Task<ServiceResult> Heartbeat(Guid incidentId, LeaseHeartbeatRequest request, CancellationToken cancellationToken);
        Task<ServiceResult> Release(Guid incidentId, ReleaseLeaseRequest request, CancellationToken cancellationToken);
        Task<ServiceResult> UpsertElement(Guid incidentId, UpsertMapElementRequest request, CancellationToken cancellationToken);
        Task<ServiceResult> DeleteElement(Guid incidentId, Guid elementId, DeleteMapElementRequest request, CancellationToken cancellationToken);
        Task<ServiceResult> GetAudit(Guid incidentId, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);
        Task<ServiceResult> ExportAuditCsv(Guid incidentId, CancellationToken cancellationToken);
        Task<ServiceResult> ExportMapPdf(Guid incidentId, CancellationToken cancellationToken);
    }
}
