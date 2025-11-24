using Microsoft.AspNetCore.Mvc;
using PullRequest_Service.Models;
using PullRequest_Service.Services;
using static PullRequest_Service.Models.MappingExtensions;

namespace PullRequest_Service.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class TeamController : ControllerBase
    {
        private readonly TeamService _teamService;
        public TeamController(TeamService teamService) => _teamService = teamService;

        [HttpPost("/team/add")]
        public async Task<IActionResult> AddTeam([FromBody] CreateTeamRequest request, CancellationToken ct)
        {
            var members = request.Members.Select(m => (m.UserId, m.Username, m.IsActive)).ToList();
            var (team, errorCode) = await _teamService.CreateTeamAsync(request.TeamName, members, ct);

            if (errorCode != null)
                return BadRequest(new ErrorResponse(new ErrorDetail(errorCode, "team_name already exists")));

            return Created($"/team/get?team_name={team!.TeamName}", new TeamResponse(team.ToDto()));
        }

        [HttpGet("/team/get")]
        public async Task<IActionResult> GetTeam([FromQuery(Name = "team_name")] string teamName, CancellationToken ct)
        {
            var team = await _teamService.GetTeamAsync(teamName, ct);
            if (team == null)
                return NotFound(new ErrorResponse(new ErrorDetail(ErrorCodes.NotFound, "resource not found")));
            return Ok(team.ToDto());
        }
    }
}
