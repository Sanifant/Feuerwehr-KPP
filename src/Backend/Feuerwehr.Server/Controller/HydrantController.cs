using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Server.Services;
using de.openelp.feuerwehr.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = PolicyNames.CanViewData)]
public class HydrantController(IHydrantService service) : ControllerBase
{
    public const long MaximumImportBytes = 5 * 1024 * 1024;

    [HttpGet("export")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        return File(await service.ExportAsync(cancellationToken), "text/csv; charset=utf-8", "hydranten.csv");
    }

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

        await using var stream = file.OpenReadStream();
        return this.ToActionResult(await service.ImportAsync(stream, cancellationToken));
    }

    // GET: api/<HydrantController>
    [HttpGet]
    public ActionResult<IEnumerable<Hydrant>> Get()
    {
        return this.ToActionResult(service.Get());
    }

    // GET api/<HydrantController>/5
    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        return this.ToActionResult(service.Get(id));
    }

    // POST api/<HydrantController>
    [HttpPost]
    [Authorize(Policy = PolicyNames.CanManageHydrants)]
    public IActionResult Post([FromBody] Hydrant value)
    {
        return this.ToActionResult(service.Post(value));
    }

    // PUT api/<HydrantController>/5
    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.CanManageHydrants)]
    public IActionResult Put(int id, [FromBody] Hydrant value)
    {
        return this.ToActionResult(service.Put(id, value));
    }

    // DELETE api/<HydrantController>/5
    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.CanManageHydrants)]
    public IActionResult Delete(int id)
    {
        return this.ToActionResult(service.Delete(id));
    }

}
