using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace de.openelp.feuerwehr.Api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryItemController : ControllerBase
    {
        private readonly IInventoryService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryItemController"/> class.
        /// </summary>
        /// <param name="service">The IInventoryService service.</param>
        public InventoryItemController(IInventoryService service) => _service = service;

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
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If no inventory item with the specified identifier exists.</response>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<InventoryItem> Get(Guid id)
        {
            return await _service.GetById(id);
        }

        /// <summary>
        /// Posts the specified value.
        /// </summary>
        /// <remarks>
        /// This endpoint requires authentication. Users must have the appropriate roles to create an inventory item.
        /// </remarks>
        /// <param name="value">The value.</param>
        /// <response code="400">If the value is null.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="200">If the inventory item was created successfully.</response>
        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task Post([FromBody] InventoryItem value)
        {
            if(value == null) BadRequest();

            _service.CreateItem(value);
        }

        /// <summary>
        /// Puts the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="value">The value.</param>
        /// <response code="400">If the identifier does not match the value's identifier.</response>
        /// <response code="200">If the inventory item was updated successfully.</response>
        /// <response code="401">If the user is not authenticated.</response>
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

        /// <summary>
        /// Deletes the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <response code="400">If the identifier is invalid.</response>
        /// <response code="200">If the inventory item was deleted successfully.</response>
        /// <response code="401">If the user is not authenticated.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "User")]
        public async Task Delete(Guid id)
        {
            _service.DeleteItem(id);
        }

        /// <summary>
        /// Gets the categories.
        /// </summary>
        /// <returns>List of all Categories.</returns>
        /// <response code="200">Returns the list of inventory categories.</response>
        /// <response code="401">If the user is not authenticated.</response>
        [HttpGet("categories")]
        [Authorize]
        public async Task<IEnumerable<InventoryCategory>> GetCategories()
        {
            return await _service.GetCategories();
        }
    }
}
