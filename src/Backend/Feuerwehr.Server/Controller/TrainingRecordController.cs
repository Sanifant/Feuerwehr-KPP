using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    /// <summary>
    /// Exposes HTTP endpoints for training course operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The training record service handling the requested operations.</param>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PolicyNames.CanViewData)]
    public class TrainingRecordController(ITrainingRecordService service) : ControllerBase
    {

        /// <summary>
        /// Retrieves training course records visible to the caller.
        /// </summary>
        /// <param name="level">The training qualification level.</param>
        /// <param name="title">The title to include in the response or exported document.</param>
        /// <returns>A task whose result is an HTTP response containing the matching training course records, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrainingCourseDto>>> Get([FromQuery] string? level, [FromQuery] string? title)
        {
            return this.ToActionResult(await service.Get(level, title));
        }

        /// <summary>
        /// Retrieves training course records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the training course to retrieve.</param>
        /// <returns>A task whose result is an HTTP response containing the matching training course record, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<TrainingCourseDto>> Get(int id)
        {
            return this.ToActionResult(await service.Get(id));
        }

        /// <summary>
        /// Creates training course records from the validated input.
        /// </summary>
        /// <param name="value">The training course data value to process.</param>
        /// <returns>A task whose result is an HTTP response containing the saved training course record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost]
        [Authorize(Policy = PolicyNames.CanManageTraining)]
        public async Task<ActionResult<TrainingCourseDto>> Post([FromBody] TrainingCourseDto value)
        {
            return this.ToActionResult(await service.Post(value));
        }

        /// <summary>
        /// Updates training course records matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the training course to process.</param>
        /// <param name="value">The training course data value to process.</param>
        /// <returns>A task whose result is an HTTP response containing the saved training course record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = PolicyNames.CanManageTraining)]
        public async Task<ActionResult> Put(int id, [FromBody] TrainingCourseDto value)
        {
            return this.ToActionResult(await service.Put(id, value));
        }

        /// <summary>
        /// Deletes the training course record matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the training course to process.</param>
        /// <returns>A task whose result is an HTTP response containing confirmation that the record was deleted, or validation, authorization, or conflict details on failure.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = PolicyNames.CanManageTraining)]
        public async Task<ActionResult> Delete(int id)
        {
            return this.ToActionResult(await service.Delete(id));
        }
    }
}
