namespace De.Openelp.Feuerwehr.Common.Models.Dto
{
    public record IncidentChangedEvent(Guid IncidentId, long Revision, DateTime ServerUtc);
}
