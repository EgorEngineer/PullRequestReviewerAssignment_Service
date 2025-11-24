using Microsoft.EntityFrameworkCore;
using PullRequest_Service.Core.Entities;
using PullRequest_Service.Infrastructure.Data;

namespace PullRequest_Service;

public class UserService
{
    private readonly ApplicationDbContext _db;
    public UserService(ApplicationDbContext db) => _db = db;

    public async Task<User?> SetIsActiveAsync(string userId, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Team).FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null) return null;
        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<List<PullRequest>> GetUserReviewsAsync(string userId, CancellationToken ct = default) =>
        await _db.PullRequests.Include(p => p.ReviewerAssignments)
            .Where(p => p.ReviewerAssignments.Any(r => r.ReviewerId == userId)).ToListAsync(ct);
}
