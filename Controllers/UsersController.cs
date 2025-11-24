using Microsoft.AspNetCore.Mvc;
using PullRequest_Service.Models;
using PullRequest_Service.Services;


namespace PullRequest_Service.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class UsersController : Controller
    {
        private readonly UserService _userService;
        public UsersController(UserService userService) => _userService = userService;

        [HttpPost("/users/setIsActive")]
        public async Task<IActionResult> SetIsActive([FromBody] SetIsActiveRequest request, CancellationToken ct)
        {
            var user = await _userService.SetIsActiveAsync(request.UserId, request.IsActive, ct);
            if (user == null)
                return NotFound(new ErrorResponse(new ErrorDetail(ErrorCodes.NotFound, "resource not found")));
            return Ok(new UserResponse(user.ToDto()));
        }

        [HttpGet("/users/getReview")]
        public async Task<IActionResult> GetReviews([FromQuery(Name = "user_id")] string userId, CancellationToken ct)
        {
            var pullRequests = await _userService.GetUserReviewsAsync(userId, ct);
            return Ok(new UserReviewsResponse(userId, pullRequests.Select(pr => pr.ToShortDto()).ToList()));
        }
    }
}
