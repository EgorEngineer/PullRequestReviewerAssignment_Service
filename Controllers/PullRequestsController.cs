using Microsoft.AspNetCore.Mvc;
using PullRequest_Service.Models;
using PullRequest_Service.Services;

namespace PullRequest_Service.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class PullRequestController : Controller
    {
        private readonly PullRequestService _prService;
        public PullRequestController(PullRequestService prService) => _prService = prService;

        [HttpPost("/pullRequest/create")]
        public async Task<IActionResult> Create([FromBody] CreatePullRequestRequest request, CancellationToken ct)
        {
            var (pr, errorCode) =
                await _prService.CreateAsync(request.PullRequestId, request.PullRequestName, request.AuthorId, ct);

            if (errorCode == "PR_EXISTS")
                return Conflict(new ErrorResponse(new ErrorDetail(errorCode, "PR id already exists")));
            if (errorCode == "NOT_FOUND")
                return NotFound(new ErrorResponse(new ErrorDetail(errorCode, "resource not found")));

            return Created($"/pullRequest/{pr!.PullRequestId}", new PullRequestResponse(pr.ToDto()));
        }

        [HttpPost("/pullRequest/merge")]
        public async Task<IActionResult> Merge([FromBody] MergePullRequestRequest request, CancellationToken ct)
        {
            var (pr, errorCode) = await _prService.MergeAsync(request.PullRequestId, ct);
            if (errorCode == "NOT_FOUND")
                return NotFound(new ErrorResponse(new ErrorDetail(errorCode, "resource not found")));
            return Ok(new PullRequestResponse(pr!.ToDto()));
        }

        [HttpPost("/pullRequest/reassign")]
        public async Task<IActionResult> Reassign([FromBody] ReassignReviewerRequest request, CancellationToken ct)
        {
            var (pr, newReviewerId, errorCode) =
                await _prService.ReassignAsync(request.PullRequestId, request.OldUserId, ct);

            return errorCode switch
            {
                "NOT_FOUND" => NotFound(new ErrorResponse(new ErrorDetail(errorCode, "resource not found"))),
                "PR_MERGED" => Conflict(new ErrorResponse(new ErrorDetail(errorCode, "cannot reassign on merged PR"))),
                "NOT_ASSIGNED" => Conflict(
                    new ErrorResponse(new ErrorDetail(errorCode, "reviewer is not assigned to this PR"))),
                "NO_CANDIDATE" => Conflict(
                    new ErrorResponse(new ErrorDetail(errorCode, "no active replacement candidate in team"))),
                _ => Ok(new ReassignResponse(pr!.ToDto(), newReviewerId!))
            };
        }
    }
}
