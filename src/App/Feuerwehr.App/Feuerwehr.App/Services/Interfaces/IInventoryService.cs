using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.App.Services
{
    public interface IInventoryService
    {
        Task<EquipmentDetailDto> FindBarcodeAsync(string barcode, CancellationToken ct);
        Task<EquipmentDetailDto> InspectAsync(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct);
    }
}
