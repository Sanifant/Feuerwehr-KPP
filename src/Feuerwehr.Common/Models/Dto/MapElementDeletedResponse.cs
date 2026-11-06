namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record MapElementDeletedResponse(bool Deleted, Guid ElementId, long IncidentRevision, DateTime ServerUtc);
}
