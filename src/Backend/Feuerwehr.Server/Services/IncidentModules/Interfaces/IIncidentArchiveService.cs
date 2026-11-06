namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public interface IIncidentArchiveService
    {
        Task<ServiceResult> GetDiary(Guid incidentId, CancellationToken cancellationToken);

        Task<ServiceResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken);
    }
}
