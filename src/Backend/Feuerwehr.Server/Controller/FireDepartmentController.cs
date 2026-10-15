using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = PolicyNames.CanViewData)]
public class FireDepartmentController(IFireDepartmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FireDepartmentDto>>> Get() =>
        this.ToActionResult(await service.Get());

    [HttpGet("{id}")]
    public async Task<ActionResult<FireDepartmentDto>> Get(int id) =>
        this.ToActionResult(await service.Get(id));

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireCommander)]
    public async Task<ActionResult<FireDepartmentDto>> Add([FromBody] FireDepartmentDto value) =>
        this.ToActionResult(await service.Add(value));
}
