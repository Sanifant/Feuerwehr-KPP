using De.Openelp.Feuerwehr.Common.Models;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    /// <summary>
    /// Exposes HTTP endpoints for hydrant operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The hydrant service handling the requested operations.</param>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PolicyNames.CanViewData)]
    public class HydrantController(IHydrantService service) : ControllerBase
    {
        /// <summary>
        /// The maximum accepted upload size in bytes.
        /// </summary>
        public const long MaximumImportBytes = 5 * 1024 * 1024;

        /// <summary>
        /// Exports hydrant records in the supported download format.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("export")]
        [Authorize(Policy = PolicyNames.RequireAdmin)]
        public async Task<IActionResult> Export(CancellationToken cancellationToken)
        {
            return this.File(await service.ExportAsync(cancellationToken), "text/csv; charset=utf-8", "hydranten.csv");
        }

        /// <summary>
        /// Validates and imports hydrant records, rejecting invalid input before applying changes.
        /// </summary>
        /// <param name="file">The uploaded import file; null when no file was supplied.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the created and updated record counts and any import errors, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("import")]
        [Authorize(Policy = PolicyNames.RequireAdmin)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaximumImportBytes + (64 * 1024))]
        public async Task<IActionResult> Import([FromForm] IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return this.BadRequest(new HydrantImportResponse(0, [new (null, "Bitte eine CSV-Datei auswählen.")]));
            }

            if (file.Length > MaximumImportBytes)
            {
                return this.BadRequest(new HydrantImportResponse(0, [new (null, "Die CSV-Datei darf höchstens 5 MB groß sein.")]));
            }

            await using Stream stream = file.OpenReadStream();
            return this.ToActionResult(await service.ImportAsync(stream, cancellationToken));
        }

        /// <summary>
        /// Retrieves hydrant records visible to the caller.
        /// </summary>
        /// <returns>An HTTP response containing the matching hydrant records, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet]
        public ActionResult<IEnumerable<Hydrant>> Get()
        {
            return this.ToActionResult(service.Get());
        }

        /// <summary>
        /// Retrieves hydrant records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to retrieve.</param>
        /// <returns>An HTTP response containing the matching hydrant record, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return this.ToActionResult(service.Get(id));
        }

        /// <summary>
        /// Creates a hydrant.
        /// </summary>
        /// <param name="value">The hydrant value to process.</param>
        /// <returns>An HTTP response containing the saved hydrant record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost]
        [Authorize(Policy = PolicyNames.CanManageHydrants)]
        public IActionResult Post([FromBody] Hydrant value)
        {
            return this.ToActionResult(service.Post(value));
        }

        /// <summary>
        /// Updates a hydrant.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to process.</param>
        /// <param name="value">The hydrant value to process.</param>
        /// <returns>An HTTP response containing the saved hydrant record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = PolicyNames.CanManageHydrants)]
        public IActionResult Put(int id, [FromBody] Hydrant value)
        {
            return this.ToActionResult(service.Put(id, value));
        }

        /// <summary>
        /// Deletes a hydrant.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to process.</param>
        /// <returns>An HTTP response containing confirmation that the record was deleted, or validation, authorization, or conflict details on failure.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = PolicyNames.CanManageHydrants)]
        public IActionResult Delete(int id)
        {
            return this.ToActionResult(service.Delete(id));
        }
    }
}
