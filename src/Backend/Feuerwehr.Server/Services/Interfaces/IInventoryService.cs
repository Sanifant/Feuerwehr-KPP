using De.Openelp.Feuerwehr.Common.Models.Inventory;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the equipment operations required by application consumers.
    /// </summary>
    public interface IInventoryService
    {
        /// <summary>
        /// Retrieves inventory categories visible to the caller.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the inventory categories, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetCategories(CancellationToken ct);

        /// <summary>
        /// Creates inventory categories from the validated input.
        /// </summary>
        /// <param name="request">The create inventory category input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the newly created category, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> CreateCategory(CreateInventoryCategoryRequest request, CancellationToken ct);

        /// <summary>
        /// Retrieves equipment types and their inspection plans visible to the caller.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the equipment types and inspection plans, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetTypes(CancellationToken ct);

        /// <summary>
        /// Creates or updates equipment types and their inspection plans after validating input and concurrency state.
        /// </summary>
        /// <param name="id">The existing equipment type identifier, or null to create a new record.</param>
        /// <param name="request">The save equipment type input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the saved equipment type and inspection plan, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> SaveType(Guid? id, SaveEquipmentTypeRequest request, CancellationToken ct);

        /// <summary>
        /// Retrieves equipment records visible to the caller.
        /// </summary>
        /// <param name="includeInactive">Whether to include deactivated records in the result.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the matching equipment records, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetAll(bool includeInactive, CancellationToken ct);

        /// <summary>
        /// Retrieves equipment records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the equipment to retrieve.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the matching equipment record, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Get(Guid id, CancellationToken ct);

        /// <summary>
        /// Looks up equipment by its unique barcode and loads its inspection history.
        /// </summary>
        /// <param name="barcode">The unique equipment barcode used for lookup and import matching.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the equipment details and inspection history, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> FindBarcode(string barcode, CancellationToken ct);

        /// <summary>
        /// Generates the equipment barcode as an SVG label suitable for printing.
        /// </summary>
        /// <param name="id">The identifier of the equipment to process.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> BarcodeLabel(Guid id, CancellationToken ct);

        /// <summary>
        /// Creates or updates equipment records after validating input and concurrency state.
        /// </summary>
        /// <param name="id">The existing equipment identifier, or null to create a new record.</param>
        /// <param name="request">The save equipment input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the saved equipment record or success status, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Save(Guid? id, SaveEquipmentRequest request, CancellationToken ct);

        /// <summary>
        /// Changes the activation state of equipment records while preserving existing history.
        /// </summary>
        /// <param name="id">The identifier of the equipment to process.</param>
        /// <param name="request">The set equipment active input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the updated activation state, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> SetActive(Guid id, SetEquipmentActiveRequest request, CancellationToken ct);

        /// <summary>
        /// Records an equipment inspection with concurrency checking and a snapshot of its inspection plan.
        /// </summary>
        /// <param name="id">The identifier of the equipment to process.</param>
        /// <param name="request">The record equipment inspection input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the equipment details and inspection history, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Inspect(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct);
    }
}
