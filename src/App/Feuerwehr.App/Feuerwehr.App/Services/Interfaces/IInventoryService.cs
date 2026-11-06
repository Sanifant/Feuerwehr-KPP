using System;
using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Defines the equipment operations required by application consumers.
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>
        /// Looks up equipment by its unique barcode and loads its inspection history.
        /// </summary>
        /// <param name="barcode">The unique equipment barcode used for lookup and import matching.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is the equipment's current details and inspection history.</returns>
        Task<EquipmentDetailDto> FindBarcodeAsync(string barcode, CancellationToken ct);

        /// <summary>
        /// Records an equipment inspection with concurrency checking and a snapshot of its inspection plan.
        /// </summary>
        /// <param name="id">The identifier of the equipment to process.</param>
        /// <param name="request">The record equipment inspection input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is the equipment's current details and inspection history.</returns>
        Task<EquipmentDetailDto> InspectAsync(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct);
    }
}
