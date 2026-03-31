using de.openelp.feuerwehr.Api;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private const string Healthy = "Healthy";
        private const string Unhealthy = "Unhealthy";

        private readonly IDatabaseHealthChecker _databaseHealthChecker;

        public StatusController(IDatabaseHealthChecker databaseHealthChecker)
        {
            _databaseHealthChecker = databaseHealthChecker;
        }

        // GET api/status
        [HttpGet]
        public async Task<ActionResult<StatusResponse>> Get()
        {
            var dbHealthy = await _databaseHealthChecker.CanConnectAsync();

            var response = new StatusResponse
            {
                Status = dbHealthy ? Healthy : Unhealthy,
                Database = dbHealthy ? Healthy : Unhealthy
            };

            return dbHealthy ? Ok(response) : StatusCode(503, response);
        }
    }
}
