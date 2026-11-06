using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    /// <summary>
    /// Exposes HTTP endpoints for fire department operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The fire department service handling the requested operations.</param>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PolicyNames.CanViewData)]
    public class FireDepartmentController(IFireDepartmentService service) : ControllerBase
    {
        /// <summary>
        /// Retrieves fire department records visible to the caller.
        /// </summary>
        /// <returns>A task whose result is an HTTP response containing the matching fire department records, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FireDepartmentDto>>> Get() =>
            this.ToActionResult(await service.Get());

        /// <summary>
        /// Retrieves fire department records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the fire department to retrieve.</param>
        /// <returns>A task whose result is an HTTP response containing the matching fire department record, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<FireDepartmentDto>> Get(int id) =>
            this.ToActionResult(await service.Get(id));

        /// <summary>
        /// Creates fire department records from the validated input.
        /// </summary>
        /// <param name="value">The fire department data value to process.</param>
        /// <returns>A task whose result is an HTTP response containing the saved fire department record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost]
        [Authorize(Policy = PolicyNames.RequireCommander)]
        public async Task<ActionResult<FireDepartmentDto>> Add([FromBody] FireDepartmentDto value) =>
            this.ToActionResult(await service.Add(value));
    }
}
