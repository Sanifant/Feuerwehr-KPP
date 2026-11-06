namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record MapElementChangedEvent(Guid IncidentId, long IncidentRevision, Guid ElementId, string Action, DateTime ServerUtc);
}
