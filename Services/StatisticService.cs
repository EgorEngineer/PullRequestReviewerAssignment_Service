using Microsoft.EntityFrameworkCore;
using PullRequest_Service.Infrastructure.Data;

namespace PullRequest_Service.Services;

public class StatisticsService
{
    private readonly ApplicationDbContext _db;

    public StatisticsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<object> GetStatisticsAsync()
    {
        var totalPR = await _db.PullRequests.CountAsync();

        var prByStatus = await _db.PullRequests
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var assignmentsByUser = await _db.PullRequestReviewers
            .GroupBy(r => r.ReviewerId)
            .Select(g => new { ReviewerId = g.Key, Count = g.Count() })
            .ToListAsync();

        var assignmentsByPR = await _db.PullRequestReviewers
            .GroupBy(r => r.PullRequestId)
            .Select(g => new { PullRequestId = g.Key, Count = g.Count() })
            .ToListAsync();

        var avgReviewersPerPR = assignmentsByPR.Count == 0
            ? 0
            : assignmentsByPR.Average(x => x.Count);

        var topReviewers = assignmentsByUser
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        var prWithoutReviewers = await _db.PullRequests
            .CountAsync(pr => !_db.PullRequestReviewers.Any(r => r.PullRequestId == pr.PullRequestId));

        var avgPRperUser = await _db.Users.AnyAsync()
            ? await _db.PullRequests.CountAsync() / (double)await _db.Users.CountAsync()
            : 0;

        var usersByTeams = await _db.Users
            .GroupBy(u => u.TeamName)
            .Select(g => new { Team = g.Key, Count = g.Count() })
            .ToListAsync();

        return new
        {
            pull_requests_total = totalPR,
            pull_requests_by_status = prByStatus,
            reviewers_assignments_by_user = assignmentsByUser,
            reviewers_assignments_by_pr = assignmentsByPR,
            average_reviewers_per_pr = avgReviewersPerPR,
            top_reviewers = topReviewers,
            pull_requests_without_reviewers = prWithoutReviewers,
            average_pr_per_user = avgPRperUser,
            users_by_team = usersByTeams
        };
    }
}
