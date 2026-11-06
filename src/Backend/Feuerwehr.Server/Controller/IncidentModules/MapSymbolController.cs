using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller.IncidentModules
{
    /// <summary>
    /// Exposes HTTP endpoints for map symbol operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The map symbol service handling the requested operations.</param>
    [ApiController]
    [Route("api/map-symbols")]
    [Authorize(Policy = PolicyNames.CanViewMapSymbols)]
    public sealed class MapSymbolController(IMapSymbolService service) : ControllerBase
    {
        /// <summary>
        /// Retrieves the available situation-map symbols.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the available map symbol definitions, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet]
        public async Task<IActionResult> GetSymbols(CancellationToken cancellationToken)
            => this.ToActionResult(await service.GetSymbols(cancellationToken));

        /// <summary>
        /// Retrieves map symbol categories visible to the caller.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the map symbol categories, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
            => this.ToActionResult(await service.GetCategories(cancellationToken));

        /// <summary>
        /// Creates map symbol categories from the validated input.
        /// </summary>
        /// <param name="request">The create map symbol category input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the newly created category, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("categories")]
        [Authorize(Policy = PolicyNames.RequireAdmin)]
        public async Task<IActionResult> CreateCategory(CreateMapSymbolCategoryRequest request, CancellationToken cancellationToken)
            => this.ToActionResult(await service.CreateCategory(request, cancellationToken));

        /// <summary>
        /// Creates map symbol records from the validated input.
        /// </summary>
        /// <param name="request">The save map symbol input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved map symbol record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost]
        [Authorize(Policy = PolicyNames.RequireAdmin)]
        [RequestSizeLimit(2097152)]
        public async Task<IActionResult> Create(SaveMapSymbolRequest request, CancellationToken cancellationToken)
            => this.ToActionResult(await service.Save(null, request, cancellationToken));

        /// <summary>
        /// Updates map symbol records matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the map symbol to process.</param>
        /// <param name="request">The save map symbol input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved map symbol record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = PolicyNames.RequireAdmin)]
        [RequestSizeLimit(2097152)]
        public async Task<IActionResult> Update(Guid id, SaveMapSymbolRequest request, CancellationToken cancellationToken)
            => this.ToActionResult(await service.Save(id, request, cancellationToken));
    }
}
