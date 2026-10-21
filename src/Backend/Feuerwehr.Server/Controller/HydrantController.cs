using de.openelp.feuerwehr.Common.Models;
using de.openelp.feuerwehr.Server.Services;
using de.openelp.feuerwehr.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = PolicyNames.CanViewData)]
public class HydrantController(IHydrantService service) : ControllerBase
{

    // GET: api/<HydrantController>
    [HttpGet]
    public ActionResult<IEnumerable<Hydrant>> Get()
    {
        return this.ToActionResult(service.Get());
    }

    // GET api/<HydrantController>/5
    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        return this.ToActionResult(service.Get(id));
    }

    // POST api/<HydrantController>
    [HttpPost]
    [Authorize(Policy = PolicyNames.CanManageHydrants)]
    public IActionResult Post([FromBody] Hydrant value)
    {
        return this.ToActionResult(service.Post(value));
    }

    // PUT api/<HydrantController>/5
    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.CanManageHydrants)]
    public IActionResult Put(int id, [FromBody] Hydrant value)
    {
        return this.ToActionResult(service.Put(id, value));
    }

    // DELETE api/<HydrantController>/5
    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.CanManageHydrants)]
    public IActionResult Delete(int id)
    {
        return this.ToActionResult(service.Delete(id));
    }

}
