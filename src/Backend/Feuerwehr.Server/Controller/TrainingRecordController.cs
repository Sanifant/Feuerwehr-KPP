using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Server.Services;
using de.openelp.feuerwehr.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = PolicyNames.CanViewData)]
public class TrainingRecordController(ITrainingRecordService service) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrainingCourseDto>>> Get([FromQuery] string? level, [FromQuery] string? title)
    {
        return this.ToActionResult(await service.Get(level, title));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TrainingCourseDto>> Get(int id)
    {
        return this.ToActionResult(await service.Get(id));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CanManageTraining)]
    public async Task<ActionResult<TrainingCourseDto>> Post([FromBody] TrainingCourseDto value)
    {
        return this.ToActionResult(await service.Post(value));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.CanManageTraining)]
    public async Task<ActionResult> Put(int id, [FromBody] TrainingCourseDto value)
    {
        return this.ToActionResult(await service.Put(id, value));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.CanManageTraining)]
    public async Task<ActionResult> Delete(int id)
    {
        return this.ToActionResult(await service.Delete(id));
    }
}
