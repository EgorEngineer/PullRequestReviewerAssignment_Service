using Microsoft.AspNetCore.Mvc;
using PullRequest_Service.Services;

namespace PullRequest_Service.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class HealthController : Controller
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        /// <summary>
        /// GET /health - Проверка работоспособности сервиса
        /// </summary>
        [HttpGet("/health")]
        [ProducesResponseType(typeof(HealthCheckResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Health(CancellationToken ct)
        {
            var result = await _healthCheckService.CheckHealthAsync(ct);

            if (!result.IsHealthy)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
            }

            return Ok(result);
        }
    }
}

