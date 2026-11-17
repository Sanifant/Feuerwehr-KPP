using de.openelp.feuerwehr.Server.Services.VehicleModules;

namespace de.openelp.feuerwehr.Server.Services
{
    public interface IVehicleTransferService
    {
        Task<ServiceResult> Export(VehicleTransferKind kind, CancellationToken ct);
        Task<ServiceResult> Import(VehicleTransferKind kind, Stream stream, CancellationToken ct);
    }
}
