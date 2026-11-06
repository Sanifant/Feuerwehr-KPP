namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Carries incident summary data across the API boundary.
    /// </summary>
    /// <param name="Id">The unique identifier of the incident summary.</param>
    /// <param name="Name">The display name of the incident summary.</param>
    /// <param name="IncidentNumber">The incident's external reference number.</param>
    /// <param name="Keyword">The incident dispatch keyword.</param>
    /// <param name="Description">The descriptive text for the incident summary.</param>
    /// <param name="OriginLongitude">The map origin longitude in decimal degrees.</param>
    /// <param name="OriginLatitude">The map origin latitude in decimal degrees.</param>
    /// <param name="Status">The current state of the incident summary.</param>
    /// <param name="CreatedAtUtc">The UTC time at which the record was created.</param>
    /// <param name="AlarmedAtUtc">The UTC time at which the incident alarm was raised.</param>
    /// <param name="ClosedAtUtc">The UTC time at which the incident was closed.</param>
    /// <param name="Revision">The revision counter for the incident summary.</param>
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
