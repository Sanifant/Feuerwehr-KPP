using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PolicyNames.CanViewData)]
    public class FireDepartmentController(IFireDepartmentService service) : ControllerBase
    {
        [HttpGet]
        /// <summary>
        /// Gets the operation.
        /// </summary>
        public async Task<ActionResult<IEnumerable<FireDepartmentDto>>> Get()
        {
            return this.ToActionResult(await service.Get());
        }

        [HttpGet("{id}")]
        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        public async Task<ActionResult<FireDepartmentDto>> Get(int id)
        {
            return this.ToActionResult(await service.Get(id));
        }

        [HttpPost]
        [Authorize(Policy = PolicyNames.RequireCommander)]
        /// <summary>
        /// Adds the operation.
        /// </summary>
        /// <param name="value">The value.</param>
        public async Task<ActionResult<FireDepartmentDto>> Add([FromBody] FireDepartmentDto value)
        {
            return this.ToActionResult(await service.Add(value));
        }
    }

}
