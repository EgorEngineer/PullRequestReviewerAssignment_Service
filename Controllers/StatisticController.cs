using Microsoft.AspNetCore.Mvc;
using PullRequest_Service.Services;

namespace PullRequest_Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : Controller
{
    private readonly StatisticsService _statisticsService;

    public StatisticsController(StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatistics()
    {
        var stats = await _statisticsService.GetStatisticsAsync();
        return Ok(stats);
    }
}