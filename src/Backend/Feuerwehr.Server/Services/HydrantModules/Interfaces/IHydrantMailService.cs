using de.openelp.feuerwehr.Server.Models.Hydrant;

namespace de.openelp.feuerwehr.Server.Services.HydrantModules.Interfaces
{
    public interface IHydrantMailService
    {
        Task QueueMailAsync(HydrantInspection entity, CancellationToken stoppingToken);
    }
}
