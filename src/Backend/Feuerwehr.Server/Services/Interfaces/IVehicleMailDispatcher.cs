namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IVehicleMailDispatcher
    {
        Task Dispatch(CancellationToken ct);
    }
}
