using PullRequest_Service.Core.Entities;

namespace PullRequest_Service;

public class PullRequestReviewer
{
    public string PullRequestId { get; set; } = string.Empty;
    public PullRequest PullRequest { get; set; } = null!;
    public string ReviewerId { get; set; } = string.Empty;
    public User Reviewer { get; set; } = null!;
}
