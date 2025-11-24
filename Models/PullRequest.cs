namespace PullRequest_Service.Core.Entities
{
    public enum PullRequestStatus
    {
        Open,
        Merged
    }
    public class PullRequest
    {
        public string PullRequestId { get; set; } = string.Empty;
        public string PullRequestName { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public User Author { get; set; } = null!;
        public PullRequestStatus Status { get; set; } = PullRequestStatus.Open;
        public ICollection<PullRequestReviewer> ReviewerAssignments { get; set; } = new List<PullRequestReviewer>();
        public DateTime? CreatedAt { get; set; }
        public DateTime? MergedAt { get; set; }
    }
}

