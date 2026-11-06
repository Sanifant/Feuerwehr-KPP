namespace de.openelp.feuerwehr.Server.Services.IncidentModules
{
    public interface IIncidentArchiveService
    {
        Task<ServiceResult> GetDiary(Guid incidentId, CancellationToken cancellationToken);
        Task<ServiceResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken);
    }
}
