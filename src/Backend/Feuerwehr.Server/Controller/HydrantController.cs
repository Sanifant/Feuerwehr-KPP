using Feuerwehr.Common.Models;
using Feuerwehr.Server.Authorization;
using Feuerwehr.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Feuerwehr.Server.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = PolicyNames.CanViewData)]
    public class HydrantController : ControllerBase
    {

        private readonly FeuerwehrDbContext _context;

        public HydrantController(FeuerwehrDbContext context)
        {
            _context = context;
        }

        // GET: api/<HydrantController>
        [HttpGet]
        public IEnumerable<Hydrant> Get()
        {
            return _context.Hydrants.ToList();
        }

        // GET api/<HydrantController>/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            Hydrant hydrant = _context.Hydrants.Find(id);
            if (hydrant == null)
            {
                return NotFound();
            }

            return Ok(hydrant);
        }

        // POST api/<HydrantController>
        [HttpPost]
        [Authorize(Policy = PolicyNames.CanManageHydrants)]
        public IActionResult Post([FromBody] Hydrant value)
        {

            _context.Hydrants.Add(value);
            _context.SaveChanges();
            return CreatedAtAction(nameof(Get), new { id = value.Id }, value);
        }

        // PUT api/<HydrantController>/5
        [HttpPut("{id}")]
        [Authorize(Policy = PolicyNames.CanManageHydrants)]
        public IActionResult Put(int id, [FromBody] Hydrant value)
        {

            var hydrant = _context.Hydrants.Find(id);
            if (hydrant == null)
            {
                return NotFound();
            }

            _context.Hydrants.Remove(hydrant);
            _context.Hydrants.Add(value);
            _context.SaveChanges();

            return NoContent();
        }

        // DELETE api/<HydrantController>/5
        [HttpDelete("{id}")]
        [Authorize(Policy = PolicyNames.CanManageHydrants)]
        public IActionResult Delete(int id)
        {
            var hydrant = _context.Hydrants.Find(id);
            if (hydrant == null)
            {
                return NotFound();
            }

            _context.Hydrants.Remove(hydrant);
            _context.SaveChanges();

            return NoContent();
        }
    }
}
