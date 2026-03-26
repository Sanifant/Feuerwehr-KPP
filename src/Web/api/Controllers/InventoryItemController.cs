using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.domain;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace de.openelp.feuerwehr.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryItemController : ControllerBase
    {
        IInventoryService _service;

        public InventoryItemController(IInventoryService service)
        {
            _service = service;
        }

        // GET: api/<InventoryItemController>
        [HttpGet]
        public IEnumerable<InventoryItem> Get()
        {
            return _service.GetAll().Result;
        }

        // GET api/<InventoryItemController>/5
        [HttpGet("{id}")]
        public InventoryItem Get(Guid id)
        {
            return _service.GetAll().Result.FirstOrDefault(i => i.Id == id);
        }

        // POST api/<InventoryItemController>
        [HttpPost]
        public async void Post([FromBody] InventoryItem value)
        {
            _service.CreateItem(value);
        }

        // PUT api/<InventoryItemController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] InventoryItem value)
        {

        }

        // DELETE api/<InventoryItemController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
