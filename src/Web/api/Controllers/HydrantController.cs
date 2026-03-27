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
            try
            {
                return Ok(_service.GetById(id));
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
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
            value.Id = id;
            try
            {
                _service.UpdateHydrant(value);
                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // DELETE api/Hydrant/5
        [HttpDelete("{id}")]
        public ActionResult Delete(Guid id)
        {
            try
            {
                _service.DeleteHydrant(id);
                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }
    }
}
