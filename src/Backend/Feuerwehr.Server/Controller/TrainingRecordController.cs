using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Services;
using De.Openelp.Feuerwehr.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = PolicyNames.CanViewData)]
public class TrainingRecordController(ITrainingRecordService service) : ControllerBase
{

    [HttpGet]
    /// <summary>
    /// Gets the operation.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <param name="title">The title.</param>
    public async Task<ActionResult<IEnumerable<TrainingCourseDto>>> Get([FromQuery] string? level, [FromQuery] string? title)
    {
        return this.ToActionResult(await service.Get(level, title));
    }

    [HttpGet("{id}")]
    /// <summary>
    /// Gets the operation.
    /// </summary>
    /// <param name="id">The id.</param>
    public async Task<ActionResult<TrainingCourseDto>> Get(int id)
    {
        return this.ToActionResult(await service.Get(id));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CanManageTraining)]
    /// <summary>
    /// Executes post.
    /// </summary>
    /// <param name="value">The value.</param>
    public async Task<ActionResult<TrainingCourseDto>> Post([FromBody] TrainingCourseDto value)
    {
        return this.ToActionResult(await service.Post(value));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.CanManageTraining)]
    /// <summary>
    /// Executes put.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="value">The value.</param>
    public async Task<ActionResult> Put(int id, [FromBody] TrainingCourseDto value)
    {
        return this.ToActionResult(await service.Put(id, value));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.CanManageTraining)]
    /// <summary>
    /// Deletes the operation.
    /// </summary>
    /// <param name="id">The id.</param>
    public async Task<ActionResult> Delete(int id)
    {
        return this.ToActionResult(await service.Delete(id));
    }
}

