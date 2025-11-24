using Microsoft.EntityFrameworkCore;
using PullRequest_Service.Core.Entities;
using PullRequest_Service.Infrastructure.Data;

namespace PullRequest_Service;

public class ReviewerAssignmentService
{
    private readonly ApplicationDbContext _db;
    private static readonly Random _random = new();
    public ReviewerAssignmentService(ApplicationDbContext db) => _db = db;

    public async Task<List<User>> SelectReviewersAsync(string authorId, CancellationToken ct = default)
    {
        var author = await _db.Users.Include(u => u.Team).ThenInclude(t => t!.Members)
            .FirstOrDefaultAsync(u => u.UserId == authorId, ct);
        if (author?.Team == null) return new List<User>();
        var candidates = author.Team.Members.Where(u => u.IsActive && u.UserId != authorId).ToList();
        return PickRandom(candidates, 2);
    }

    public async Task<User?> FindReplacementAsync(string oldReviewerId, string authorId,
        IEnumerable<string> currentReviewerIds, CancellationToken ct = default)
    {
        var oldReviewer = await _db.Users.Include(u => u.Team).ThenInclude(t => t!.Members)
            .FirstOrDefaultAsync(u => u.UserId == oldReviewerId, ct);
        if (oldReviewer?.Team == null) return null;

        var excludeIds = currentReviewerIds.ToHashSet();
        excludeIds.Add(authorId);
        var candidates = oldReviewer.Team.Members.Where(u => u.IsActive && !excludeIds.Contains(u.UserId)).ToList();
        return candidates.Count > 0 ? PickRandom(candidates, 1).FirstOrDefault() : null;
    }

    private static List<User> PickRandom(List<User> source, int count) =>
        source.Count == 0 ? new List<User>() : source.OrderBy(_ => _random.Next()).Take(Math.Min(count, source.Count)).ToList();
}
