namespace PullRequest_Service.Core.Entities
{
    public class User
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? TeamName { get; set; }
        public Team? Team { get; set; }
        public ICollection<PullRequest> AuthoredPullRequests { get; set; } = new List<PullRequest>();
        public ICollection<PullRequestReviewer> ReviewAssignments { get; set; } = new List<PullRequestReviewer>();
    }
}

