namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IInventoryTransferService
    {
        Task<ServiceResult> Export(InventoryTransferKind kind, CancellationToken ct);

        Task<ServiceResult> Import(InventoryTransferKind kind, Stream stream, CancellationToken ct);
    }
}
