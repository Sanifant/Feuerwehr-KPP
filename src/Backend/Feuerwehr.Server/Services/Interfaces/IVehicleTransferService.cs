namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the vehicle transfer operations required by application consumers.
    /// </summary>
    public interface IVehicleTransferService
    {
        /// <summary>
        /// Exports vehicle transfer records in the supported download format.
        /// </summary>
        /// <param name="kind">The category of records to transfer and its corresponding file format.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Export(VehicleTransferKind kind, CancellationToken ct);

        /// <summary>
        /// Validates and imports vehicle transfer records, rejecting invalid input before applying changes.
        /// </summary>
        /// <param name="kind">The category of records to transfer and its corresponding file format.</param>
        /// <param name="stream">The readable import stream in the format selected by the transfer kind.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the created and updated record counts and any import errors, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Import(VehicleTransferKind kind, Stream stream, CancellationToken ct);
    }
}
