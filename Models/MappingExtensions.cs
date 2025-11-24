using PullRequest_Service.Core.Entities;
using PullRequest_Service.Models;

namespace PullRequest_Service.Models;

public static class MappingExtensions
{
    public static TeamMemberDto ToMemberDto(this User user)
    {
        return new TeamMemberDto(user.UserId, user.Username, user.IsActive);
    }

    public static TeamDto ToDto(this Team team)
    {
        return new TeamDto(
            team.TeamName,
            team.Members.Select(m => m.ToMemberDto()).ToList()
        );
    }

    public static UserDto ToDto(this User user)
    {
        return new UserDto(
            user.UserId,
            user.Username,
            user.TeamName ?? string.Empty,
            user.IsActive
        );
    }

    public static PullRequestDto ToDto(this PullRequest pr)
    {
        return new PullRequestDto(
            pr.PullRequestId,
            pr.PullRequestName,
            pr.AuthorId,
            pr.Status.ToString().ToUpperInvariant(),
            pr.ReviewerAssignments.Select(r => r.ReviewerId).ToList(),
            pr.CreatedAt,
            pr.MergedAt
        );
    }

    public static PullRequestShortDto ToShortDto(this PullRequest pr)
    {
        return new PullRequestShortDto(
            pr.PullRequestId,
            pr.PullRequestName,
            pr.AuthorId,
            pr.Status.ToString().ToUpperInvariant()
        );
    }
}

