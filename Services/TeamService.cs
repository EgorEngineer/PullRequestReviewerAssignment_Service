using Microsoft.EntityFrameworkCore;
using PullRequest_Service.Core.Entities;
using PullRequest_Service.Infrastructure.Data;

namespace PullRequest_Service.Services;

public class TeamService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TeamService> _logger;

    public TeamService(ApplicationDbContext db, ILogger<TeamService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(Team? Team, string? ErrorCode)> CreateTeamAsync(
        string teamName, List<(string UserId, string Username, bool IsActive)> members, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating team: {TeamName} with {MemberCount} members", teamName, members.Count);

        if (await _db.Teams.AnyAsync(t => t.TeamName == teamName, ct))
        {
            _logger.LogWarning("Team creation failed: Team '{TeamName}' already exists", teamName);
            return (null, "TEAM_EXISTS");
        }

        var team = new Team { TeamName = teamName, CreatedAt = DateTime.UtcNow };
        _db.Teams.Add(team);

        var createdUsers = 0;
        var updatedUsers = 0;

        foreach (var (userId, username, isActive) in members)
        {
            var user = await _db.Users.FindAsync(new object[] { userId }, ct);
            if (user == null)
            {
                user = new User { UserId = userId, Username = username, IsActive = isActive, TeamName = teamName };
                _db.Users.Add(user);
                createdUsers++;
            }
            else
            {
                user.Username = username;
                user.IsActive = isActive;
                user.TeamName = teamName;
                updatedUsers++;
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("✅ Team '{TeamName}' created successfully. Users created: {Created}, updated: {Updated}",
            teamName, createdUsers, updatedUsers);

        return (await GetTeamAsync(teamName, ct), null);
    }

    public async Task<Team?> GetTeamAsync(string teamName, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching team: {TeamName}", teamName);

        var team = await _db.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.TeamName == teamName, ct);

        if (team == null)
        {
            _logger.LogWarning("Team '{TeamName}' not found", teamName);
        }

        return team;
    }


    public async Task DeactivateUsersAsync(IEnumerable<string> userIds)
    {
        var usersToDeactivate = await _db.Users
            .Include(u => u.ReviewAssignments)
            .ThenInclude(r => r.PullRequest)
            .ThenInclude(pr => pr.Author)
            .ThenInclude(a => a.Team)
            .ThenInclude(t => t.Members)
            .Where(u => userIds.Contains(u.UserId) && u.IsActive)
            .ToListAsync();

        foreach (var user in usersToDeactivate)
        {
            user.IsActive = false;

            var openAssignments = user.ReviewAssignments
                .Where(r => r.PullRequest.Status == PullRequestStatus.Open)
                .ToList();

            foreach (var assignment in openAssignments)
            {
                var authorTeam = assignment.PullRequest.Author?.Team;
                if (authorTeam == null) continue;

                var activeMembers = authorTeam.Members
                    .Where(u => u.IsActive && u.UserId != user.UserId)
                    .ToList();

                if (activeMembers.Any())
                {
                    var newReviewer = activeMembers[Random.Shared.Next(activeMembers.Count)];
                    assignment.ReviewerId = newReviewer.UserId;
                }
                else
                {
                    assignment.ReviewerId = null;
                }
            }
        }

        await _db.SaveChangesAsync();
    }
}