using System;
using System.Threading;
using System.Threading.Tasks;
using de.openelp.feuerwehr.Common.Models.Inventory;

namespace de.openelp.feuerwehr.App.Services
{
    public interface IInventoryService
    {
        Task<EquipmentDetailDto> FindBarcodeAsync(string barcode, CancellationToken ct);
        Task<EquipmentDetailDto> InspectAsync(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct);
    }
}
