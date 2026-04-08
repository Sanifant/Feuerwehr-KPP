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

        /// <summary>
        /// Returns all inventory items.
        /// </summary>
        /// <remarks>
        /// This endpoint requires authentication. Users must have the appropriate roles to access the inventory items.
        /// </remarks>
        /// <returns>The list of inventory items.</returns>
        /// <response code="200">Returns the list of inventory items.</response>
        [HttpGet]
        [Authorize]
        public IEnumerable<InventoryItem> Get()
        {
            return _service.GetAll().Result;
        }

        /// <summary>
        /// Retrieves the inventory item with the specified unique identifier.
        /// </summary>
        /// <remarks>
        /// This endpoint requires authentication. Users must have the appropriate roles to access the inventory item.
        /// </remarks>
        /// <param name="id">The unique identifier of the inventory item to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the inventory item with the
        /// specified identifier.</returns>
        /// <response code="200">Returns the inventory item with the specified identifier.</response>
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
            if(value == null) BadRequest();

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

        [HttpGet("categories")]
        [Authorize]
        public async Task<IEnumerable<InventoryCategory>> GetCategories()
        {
            return await _service.GetCategories();
        }
    }
}
