using System.Globalization;
using System.Text;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public interface IInventoryService
    {
        Task<ServiceResult> GetCategories(CancellationToken ct);
        Task<ServiceResult> CreateCategory(CreateInventoryCategoryRequest request, CancellationToken ct);
        Task<ServiceResult> GetTypes(CancellationToken ct);
        Task<ServiceResult> SaveType(Guid? id, SaveEquipmentTypeRequest request, CancellationToken ct);
        Task<ServiceResult> GetAll(bool includeInactive, CancellationToken ct);
        Task<ServiceResult> Get(Guid id, CancellationToken ct);
        Task<ServiceResult> FindBarcode(string barcode, CancellationToken ct);
        Task<ServiceResult> BarcodeLabel(Guid id, CancellationToken ct);
        Task<ServiceResult> Save(Guid? id, SaveEquipmentRequest request, CancellationToken ct);
        Task<ServiceResult> SetActive(Guid id, SetEquipmentActiveRequest request, CancellationToken ct);
        Task<ServiceResult> Inspect(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct);
    }
}
