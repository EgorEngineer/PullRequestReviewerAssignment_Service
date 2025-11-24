using Microsoft.EntityFrameworkCore;
using PullRequest_Service.Core.Entities;

namespace PullRequest_Service.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<PullRequest> PullRequests => Set<PullRequest>();
    public DbSet<PullRequestReviewer> PullRequestReviewers => Set<PullRequestReviewer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Team>(e =>
        {
            e.HasKey(t => t.TeamName);
            e.Property(t => t.TeamName).HasMaxLength(200);
            e.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()");
        });


        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.UserId);
            e.Property(u => u.UserId).HasMaxLength(100);
            e.Property(u => u.Username).IsRequired().HasMaxLength(200);
            e.Property(u => u.TeamName).HasMaxLength(200);
            e.HasOne(u => u.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(u => u.TeamName)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PullRequest>(e =>
        {
            e.HasKey(p => p.PullRequestId);
            e.Property(p => p.PullRequestId).HasMaxLength(100);
            e.Property(p => p.PullRequestName).IsRequired().HasMaxLength(500);
            e.Property(p => p.AuthorId).IsRequired().HasMaxLength(100);
            e.Property(p => p.Status)
                .HasConversion(
                    v => v.ToString().ToUpperInvariant(),
                    v => Enum.Parse<PullRequestStatus>(v, true))
                .HasMaxLength(20);
            e.HasOne(p => p.Author)
                .WithMany(u => u.AuthoredPullRequests)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PullRequestReviewer>(e =>
        {
            e.HasKey(pr => new { pr.PullRequestId, pr.ReviewerId });
            e.HasOne(pr => pr.PullRequest)
                .WithMany(p => p.ReviewerAssignments)
                .HasForeignKey(pr => pr.PullRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(pr => pr.Reviewer)
                .WithMany(u => u.ReviewAssignments)
                .HasForeignKey(pr => pr.ReviewerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(pr => pr.ReviewerId);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var teams = new[]
        {
            new Team { TeamName = "Backend", CreatedAt = baseDate.AddDays(-365) },
            new Team { TeamName = "Frontend", CreatedAt = baseDate.AddDays(-350) },
            new Team { TeamName = "DevOps", CreatedAt = baseDate.AddDays(-340) },
            new Team { TeamName = "QA", CreatedAt = baseDate.AddDays(-330) },
            new Team { TeamName = "Mobile", CreatedAt = baseDate.AddDays(-320) },
            new Team { TeamName = "Data Science", CreatedAt = baseDate.AddDays(-310) },
            new Team { TeamName = "Security", CreatedAt = baseDate.AddDays(-300) },
            new Team { TeamName = "Infrastructure", CreatedAt = baseDate.AddDays(-290) },
            new Team { TeamName = "Analytics", CreatedAt = baseDate.AddDays(-280) },
            new Team { TeamName = "Platform", CreatedAt = baseDate.AddDays(-270) },
            new Team { TeamName = "Payments", CreatedAt = baseDate.AddDays(-260) },
            new Team { TeamName = "Auth", CreatedAt = baseDate.AddDays(-250) },
            new Team { TeamName = "Notifications", CreatedAt = baseDate.AddDays(-240) },
            new Team { TeamName = "Search", CreatedAt = baseDate.AddDays(-230) },
            new Team { TeamName = "Recommendations", CreatedAt = baseDate.AddDays(-220) },
            new Team { TeamName = "Core", CreatedAt = baseDate.AddDays(-210) },
            new Team { TeamName = "API", CreatedAt = baseDate.AddDays(-200) },
            new Team { TeamName = "UI-UX", CreatedAt = baseDate.AddDays(-190) },
            new Team { TeamName = "ML", CreatedAt = baseDate.AddDays(-180) },
            new Team { TeamName = "Integration", CreatedAt = baseDate.AddDays(-170) }
        };

        modelBuilder.Entity<Team>().HasData(teams);

        var users = GenerateUsers(teams);
        modelBuilder.Entity<User>().HasData(users);

        var pullRequests = GeneratePullRequests(users, baseDate);
        modelBuilder.Entity<PullRequest>().HasData(pullRequests);

        var reviewerAssignments = GenerateReviewerAssignments(pullRequests, users);
        modelBuilder.Entity<PullRequestReviewer>().HasData(reviewerAssignments);
    }

    private static List<User> GenerateUsers(Team[] teams)
    {
        var users = new List<User>();
        int userIndex = 1;

        foreach (var team in teams)
        {
            for (int i = 1; i <= 10; i++)
            {
                users.Add(new User
                {
                    UserId = $"user-{userIndex}",
                    Username = $"{team.TeamName.Replace(" ", "").Replace("-", "")}User{i}",
                    IsActive = userIndex % 10 != 0, 
                    TeamName = team.TeamName
                });
                userIndex++;
            }
        }

        return users;
    }

    private static List<PullRequest> GeneratePullRequests(List<User> users, DateTime baseDate)
    {
        var pullRequests = new List<PullRequest>();
        var activeUsers = users.Where(u => u.IsActive).ToList();

        for (int i = 1; i <= 100; i++)
        {
            var author = activeUsers[i % activeUsers.Count];
            var isOpen = i % 3 != 0; 
            var createdAt = baseDate.AddDays(-30 + i % 30);

            pullRequests.Add(new PullRequest
            {
                PullRequestId = $"pr-{i}",
                PullRequestName = $"Feature: Implement feature #{i}",
                AuthorId = author.UserId,
                Status = isOpen ? PullRequestStatus.Open : PullRequestStatus.Merged,
                CreatedAt = createdAt,
                MergedAt = isOpen ? null : createdAt.AddHours(i % 24 + 1)
            });
        }

        return pullRequests;
    }

    private static List<PullRequestReviewer> GenerateReviewerAssignments(
        List<PullRequest> pullRequests,
        List<User> users)
    {
        var assignments = new List<PullRequestReviewer>();

        foreach (var pr in pullRequests)
        {
            var author = users.First(u => u.UserId == pr.AuthorId);
            var teamMembers = users
                .Where(u => u.TeamName == author.TeamName
                    && u.UserId != pr.AuthorId
                    && u.IsActive)
                .ToList();

            if (!teamMembers.Any()) continue;
            var reviewerCount = Math.Min(teamMembers.Count, 2);
            var selectedReviewers = teamMembers.Take(reviewerCount);

            foreach (var reviewer in selectedReviewers)
            {
                assignments.Add(new PullRequestReviewer
                {
                    PullRequestId = pr.PullRequestId,
                    ReviewerId = reviewer.UserId
                });
            }
        }

        return assignments;
    }
}