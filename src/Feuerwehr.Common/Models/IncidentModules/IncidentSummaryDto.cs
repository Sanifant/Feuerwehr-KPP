namespace de.openelp.feuerwehr.Common.Models.IncidentModules
{
    public record IncidentSummaryDto(
        Guid Id,
        string Name,
        string? IncidentNumber,
        string? Keyword,
        string? Description,
        double OriginLongitude,
        double OriginLatitude,
        IncidentStatus Status,
        DateTime CreatedAtUtc,
        DateTime AlarmedAtUtc,
        DateTime? ClosedAtUtc,
        long Revision);
}
