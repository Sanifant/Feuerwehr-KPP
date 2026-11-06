namespace de.openelp.feuerwehr.Common.Models.IncidentModules
{
    public record MapAuditEventDto(
        Guid Id,
        Guid IncidentId,
        long SequenceNumber,
        string CommandId,
        Guid? ElementId,
        string Action,
        string UserId,
        string UserDisplayName,
        DateTime OccurredAtUtc,
        string BeforeStateJson,
        string AfterStateJson);
}
