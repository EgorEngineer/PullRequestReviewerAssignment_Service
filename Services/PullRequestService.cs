using Microsoft.EntityFrameworkCore;
using PullRequest_Service.Core.Entities;
using PullRequest_Service.Infrastructure.Data;

namespace PullRequest_Service.Services;

public class PullRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly ReviewerAssignmentService _reviewerService;
    private readonly ILogger<PullRequestService> _logger;

    public PullRequestService(
        ApplicationDbContext db,
        ReviewerAssignmentService reviewerService,
        ILogger<PullRequestService> logger)
    {
        _db = db;
        _reviewerService = reviewerService;
        _logger = logger;
    }

    public async Task<(PullRequest? Pr, string? ErrorCode)> CreateAsync(
        string prId, string prName, string authorId, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating PR: {PrId} by author: {AuthorId}", prId, authorId);

        if (await _db.PullRequests.AnyAsync(p => p.PullRequestId == prId, ct))
        {
            _logger.LogWarning("PR creation failed: PR '{PrId}' already exists", prId);
            return (null, "PR_EXISTS");
        }

        var author = await _db.Users.Include(u => u.Team).FirstOrDefaultAsync(u => u.UserId == authorId, ct);
        if (author == null)
        {
            _logger.LogWarning("PR creation failed: Author '{AuthorId}' not found", authorId);
            return (null, "NOT_FOUND");
        }

        var pr = new PullRequest
        {
            PullRequestId = prId,
            PullRequestName = prName,
            AuthorId = authorId,
            Status = PullRequestStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        _db.PullRequests.Add(pr);

        var reviewers = await _reviewerService.SelectReviewersAsync(authorId, ct);
        foreach (var r in reviewers)
        {
            pr.ReviewerAssignments.Add(new PullRequestReviewer { PullRequestId = prId, ReviewerId = r.UserId });
            _logger.LogDebug("Assigned reviewer {ReviewerId} to PR {PrId}", r.UserId, prId);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("✅ PR '{PrId}' created with {ReviewerCount} reviewers", prId, reviewers.Count);

        return (pr, null);
    }

    public async Task<(PullRequest? Pr, string? ErrorCode)> MergeAsync(string prId, CancellationToken ct = default)
    {
        _logger.LogInformation("Merging PR: {PrId}", prId);

        var pr = await GetByIdAsync(prId, ct);
        if (pr == null)
        {
            _logger.LogWarning("Merge failed: PR '{PrId}' not found", prId);
            return (null, "NOT_FOUND");
        }

        // Идемпотентность
        if (pr.Status == PullRequestStatus.Merged)
        {
            _logger.LogInformation("PR '{PrId}' already merged (idempotent call)", prId);
            return (pr, null);
        }

        pr.Status = PullRequestStatus.Merged;
        pr.MergedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("✅ PR '{PrId}' merged successfully", prId);

        return (pr, null);
    }

    public async Task<(PullRequest? Pr, string? NewReviewerId, string? ErrorCode)> ReassignAsync(
        string prId, string oldUserId, CancellationToken ct = default)
    {
        _logger.LogInformation("Reassigning reviewer on PR: {PrId}, old reviewer: {OldReviewerId}", prId, oldUserId);

        var pr = await GetByIdAsync(prId, ct);
        if (pr == null)
        {
            _logger.LogWarning("Reassign failed: PR '{PrId}' not found", prId);
            return (null, null, "NOT_FOUND");
        }

        if (pr.Status == PullRequestStatus.Merged)
        {
            _logger.LogWarning("Reassign failed: PR '{PrId}' is already merged", prId);
            return (null, null, "PR_MERGED");
        }

        var assignment = pr.ReviewerAssignments.FirstOrDefault(r => r.ReviewerId == oldUserId);
        if (assignment == null)
        {
            _logger.LogWarning("Reassign failed: Reviewer '{OldReviewerId}' not assigned to PR '{PrId}'", oldUserId, prId);
            return (null, null, "NOT_ASSIGNED");
        }

        var currentIds = pr.ReviewerAssignments.Select(r => r.ReviewerId);
        var newReviewer = await _reviewerService.FindReplacementAsync(oldUserId, pr.AuthorId, currentIds, ct);
        if (newReviewer == null)
        {
            _logger.LogWarning("Reassign failed: No available candidates to replace '{OldReviewerId}' on PR '{PrId}'",
                oldUserId, prId);
            return (null, null, "NO_CANDIDATE");
        }

        _db.PullRequestReviewers.Remove(assignment);
        pr.ReviewerAssignments.Add(new PullRequestReviewer { PullRequestId = prId, ReviewerId = newReviewer.UserId });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("✅ Reviewer reassigned on PR '{PrId}': {OldReviewerId} -> {NewReviewerId}",
            prId, oldUserId, newReviewer.UserId);

        pr = await GetByIdAsync(prId, ct);
        return (pr, newReviewer.UserId, null);
    }

    public async Task<PullRequest?> GetByIdAsync(string prId, CancellationToken ct = default)
    {
        return await _db.PullRequests
            .Include(p => p.ReviewerAssignments)
            .FirstOrDefaultAsync(p => p.PullRequestId == prId, ct);
    }
}