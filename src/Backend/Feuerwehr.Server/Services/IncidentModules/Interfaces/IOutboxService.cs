namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public interface IOutboxService
    {
        void AddMessage(string aggregateType, string eventType, string aggregateId, object payload);
    }
}
