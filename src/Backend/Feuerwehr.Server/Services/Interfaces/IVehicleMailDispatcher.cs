namespace de.openelp.feuerwehr.Server.Services
{
    public interface IVehicleMailDispatcher
    {
        Task Dispatch(CancellationToken ct);
    }
}
