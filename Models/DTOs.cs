using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace PullRequest_Service.Models
{
    public record ErrorResponse(ErrorDetail Error);
    public record ErrorDetail(string Code, string Message);

    public static class ErrorCodes
    {
        public const string TeamExists = "TEAM_EXISTS";
        public const string PrExists = "PR_EXISTS";
        public const string PrMerged = "PR_MERGED";
        public const string NotAssigned = "NOT_ASSIGNED";
        public const string NoCandidate = "NO_CANDIDATE";
        public const string NotFound = "NOT_FOUND";
    }

    public record TeamMemberDto(
        [property: JsonPropertyName("user_id")] string UserId,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("is_active")] bool IsActive);

    public record TeamDto(
        [property: JsonPropertyName("team_name")] string TeamName,
        [property: JsonPropertyName("members")] List<TeamMemberDto> Members);

    public record CreateTeamRequest(
        [property: JsonPropertyName("team_name")] string TeamName,
        [property: JsonPropertyName("members")] List<TeamMemberDto> Members);

    public record TeamResponse([property: JsonPropertyName("team")] TeamDto Team);

    public record UserDto(
        [property: JsonPropertyName("user_id")] string UserId,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("team_name")] string TeamName,
        [property: JsonPropertyName("is_active")] bool IsActive);

    public record SetIsActiveRequest(
        [property: JsonPropertyName("user_id")] string UserId,
        [property: JsonPropertyName("is_active")] bool IsActive);

    public record UserResponse([property: JsonPropertyName("user")] UserDto User);

    public record UserReviewsResponse(
        [property: JsonPropertyName("user_id")] string UserId,
        [property: JsonPropertyName("pull_requests")] List<PullRequestShortDto> PullRequests);

    public record PullRequestDto(
        [property: JsonPropertyName("pull_request_id")] string PullRequestId,
        [property: JsonPropertyName("pull_request_name")] string PullRequestName,
        [property: JsonPropertyName("author_id")] string AuthorId,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("assigned_reviewers")] List<string> AssignedReviewers,
        [property: JsonPropertyName("createdAt")] DateTime? CreatedAt = null,
        [property: JsonPropertyName("mergedAt")] DateTime? MergedAt = null);

    public record PullRequestShortDto(
        [property: JsonPropertyName("pull_request_id")] string PullRequestId,
        [property: JsonPropertyName("pull_request_name")] string PullRequestName,
        [property: JsonPropertyName("author_id")] string AuthorId,
        [property: JsonPropertyName("status")] string Status);

    public record CreatePullRequestRequest(
        [property: JsonPropertyName("pull_request_id")] string PullRequestId,
        [property: JsonPropertyName("pull_request_name")] string PullRequestName,
        [property: JsonPropertyName("author_id")] string AuthorId);

    public record MergePullRequestRequest([property: JsonPropertyName("pull_request_id")] string PullRequestId);

    public record ReassignReviewerRequest(
        [property: JsonPropertyName("pull_request_id")] string PullRequestId,
        [property: JsonPropertyName("old_user_id")] string OldUserId);

    public record PullRequestResponse([property: JsonPropertyName("pr")] PullRequestDto Pr);

    public record ReassignResponse(
        [property: JsonPropertyName("pr")] PullRequestDto Pr,
        [property: JsonPropertyName("replaced_by")] string ReplacedBy);
}
