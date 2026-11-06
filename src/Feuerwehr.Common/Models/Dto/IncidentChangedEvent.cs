namespace de.openelp.feuerwehr.Common.Models.Dto
{
    public record IncidentChangedEvent(Guid IncidentId, long Revision, DateTime ServerUtc);
}
