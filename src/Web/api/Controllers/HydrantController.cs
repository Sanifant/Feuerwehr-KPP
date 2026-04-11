using de.openelp.feuerwehr.application.hydrant;
using de.openelp.feuerwehr.domain;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [Route("api/[controller]")]
    [ApiController]
    public class HydrantController : ControllerBase
    {
        private readonly HydrantService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="HydrantController"/> class.
        /// </summary>
        /// <param name="service">The service.</param>
        public HydrantController(HydrantService service) => _service = service;

        /// <summary>
        /// Gets this instance.
        /// </summary>
        /// <returns>Returns the List of all Hydrants in the system.</returns>
        /// <response code="200">Returns the List of all Hydrants in the system</response>
        /// <response code="500">If an error occurred while processing the request</response>
        [HttpGet]
        public IEnumerable<Hydrant> Get()
        {
            return _service.GetAll().Result;
        }

        /// <summary>
        /// Gets the specified Hydrant.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The found Hydrant</returns>
        /// <respone code="404">If the Hydrant was not found</respone>
        /// <response code="200">If the Hydrant was found</response>
        [HttpGet("{id}")]
        public ActionResult<Hydrant> Get(Guid id)
        {
            var hydrant = _service.GetById(id);
            if (hydrant == null)
                return NotFound();
            return Ok(hydrant);
        }

        /// <summary>
        /// Posts the specified value.
        /// </summary>
        /// <param name="value">The Hydrant to be created.</param>
        /// <returns>The created Hydrant</returns>
        /// <response code="201">If the Hydrant was created successfully</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost]
        public ActionResult<Hydrant> Post([FromBody] Hydrant value)
        {
            _service.CreateHydrant(value);
            return CreatedAtAction(nameof(Get), new { id = value.Id }, value);
        }

        /// <summary>
        /// Puts the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the Hydrant to be updated.</param>
        /// <param name="value">The Hydrant to be updated.</param>
        /// <response code="204">If the Hydrant was updated successfully</response>
        /// <response code="404">If the Hydrant was not found</response>
        [HttpPut("{id}")]
        public ActionResult Put(Guid id, [FromBody] Hydrant value)
        {
            if (_service.GetById(id) == null)
                return NotFound();
            value.Id = id;
            _service.UpdateHydrant(value);
            return NoContent();
        }

        /// <summary>
        /// Deletes the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <response code="204">If the Hydrant was deleted successfully</response>
        /// <response code="404">If the Hydrant was not found</response>
        [HttpDelete("{id}")]
        public ActionResult Delete(Guid id)
        {
            if (_service.GetById(id) == null)
                return NotFound();
            _service.DeleteHydrant(id);
            return NoContent();
        }
    }
}
