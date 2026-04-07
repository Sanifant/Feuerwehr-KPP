using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace de.openelp.feuerwehr.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryItemController : ControllerBase
    {
        private readonly IInventoryService _service;

        public InventoryItemController(IInventoryService service)
        {
            _service = service;
        }

        // GET: api/<InventoryItemController>
        [HttpGet]
        [Authorize]
        public IEnumerable<InventoryItem> Get()
        {
            return _service.GetAll().Result;
        }

        // GET api/<InventoryItemController>/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<InventoryItem> Get(Guid id)
        {
            return await _service.GetById(id);
        }

        // POST api/<InventoryItemController>
        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task Post([FromBody] InventoryItem value)
        {
            _service.CreateItem(value);
        }

        // PUT api/<InventoryItemController>/5
        [HttpPut("{id}")]
        [Authorize(Roles = "User")]

        public async Task Put(Guid id, [FromBody] InventoryItem value)
        {
            if(id != value.Id)
            {
                BadRequest();
                return;
            }
            _service.UpdateItem(value);
        }

        // DELETE api/<InventoryItemController>/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "User")]
        public async Task Delete(Guid id)
        {
            _service.DeleteItem(id);
        }
    }
}
