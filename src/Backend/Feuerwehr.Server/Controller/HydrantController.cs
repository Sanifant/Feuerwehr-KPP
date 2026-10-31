using De.Openelp.Feuerwehr.Common.Models;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PolicyNames.CanViewData)]
    public class HydrantController(IHydrantService service) : ControllerBase
    {
        public const long MaximumImportBytes = 5 * 1024 * 1024;

        /// <summary>
        /// Exports hydrant data as a CSV file.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        [HttpGet("export")]
        [Authorize(Policy = PolicyNames.RequireAdmin)]
        public async Task<IActionResult> Export(CancellationToken cancellationToken)
        {
            return File(await service.ExportAsync(cancellationToken), "text/csv; charset=utf-8", "hydranten.csv");
        }

        /// <summary>
        /// Imports hydrant data from an uploaded CSV file.
        /// </summary>
        /// <param name="file">The uploaded CSV file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [HttpPost("import")]
        [Authorize(Policy = PolicyNames.RequireAdmin)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaximumImportBytes + 64 * 1024)]
        public async Task<IActionResult> Import([FromForm] IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new HydrantImportResponse(0, [new(null, "Bitte eine CSV-Datei auswählen.")]));
            if (file.Length > MaximumImportBytes)
                return BadRequest(new HydrantImportResponse(0, [new(null, "Die CSV-Datei darf höchstens 5 MB groß sein.")]));

            await using Stream stream = file.OpenReadStream();
            return this.ToActionResult(await service.ImportAsync(stream, cancellationToken));
        }

        /// <summary>
        /// Gets all hydrants.
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<Hydrant>> Get()
        {
            return this.ToActionResult(service.Get());
        }

        /// <summary>
        /// Gets a hydrant by id.
        /// </summary>
        /// <param name="id">The hydrant identifier.</param>
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            return this.ToActionResult(service.Get(id));
        }

        /// <summary>
        /// Creates a hydrant.
        /// </summary>
        /// <param name="value">The hydrant to create.</param>
        [HttpPost]
        [Authorize(Policy = PolicyNames.CanManageHydrants)]
        public IActionResult Post([FromBody] Hydrant value)
        {
            return this.ToActionResult(service.Post(value));
        }

        /// <summary>
        /// Updates a hydrant.
        /// </summary>
        /// <param name="id">The hydrant identifier.</param>
        /// <param name="value">The hydrant values to update.</param>
        [HttpPut("{id}")]
        [Authorize(Policy = PolicyNames.CanManageHydrants)]
        public IActionResult Put(int id, [FromBody] Hydrant value)
        {
            return this.ToActionResult(service.Put(id, value));
        }

        /// <summary>
        /// Deletes a hydrant.
        /// </summary>
        /// <param name="id">The hydrant identifier.</param>
        [HttpDelete("{id}")]
        [Authorize(Policy = PolicyNames.CanManageHydrants)]
        public IActionResult Delete(int id)
        {
            return this.ToActionResult(service.Delete(id));
        }

    }
}
