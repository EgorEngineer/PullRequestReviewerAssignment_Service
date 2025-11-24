namespace PullRequest_Service.Core.Entities
{
    public class Team
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public ICollection<User> Members { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
