using de.openelp.feuerwehr.application.hydrant;
using de.openelp.feuerwehr.domain;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HydrantController : ControllerBase
    {
        private readonly HydrantService _service;

        public HydrantController(HydrantService service)
        {
            _service = service;
        }

        // GET: api/Hydrant
        [HttpGet]
        public IEnumerable<Hydrant> Get()
        {
            return _service.GetAll().Result;
        }

        // GET api/Hydrant/5
        [HttpGet("{id}")]
        public ActionResult<Hydrant> Get(Guid id)
        {
            var hydrant = _service.GetById(id);
            if (hydrant == null)
                return NotFound();
            return Ok(hydrant);
        }

        // POST api/Hydrant
        [HttpPost]
        public ActionResult<Hydrant> Post([FromBody] Hydrant value)
        {
            _service.CreateHydrant(value);
            return CreatedAtAction(nameof(Get), new { id = value.Id }, value);
        }

        // PUT api/Hydrant/5
        [HttpPut("{id}")]
        public ActionResult Put(Guid id, [FromBody] Hydrant value)
        {
            if (_service.GetById(id) == null)
                return NotFound();
            value.Id = id;
            _service.UpdateHydrant(value);
            return NoContent();
        }

        // DELETE api/Hydrant/5
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
