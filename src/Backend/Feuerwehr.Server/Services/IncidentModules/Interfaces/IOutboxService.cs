namespace de.openelp.feuerwehr.Server.Services.IncidentModules
{
    public interface IOutboxService
    {
        void AddMessage(string aggregateType, string eventType, string aggregateId, object payload);
    }
}
