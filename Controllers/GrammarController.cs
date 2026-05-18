using GrammarCorrector.Models;
using GrammarCorrector.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GrammarCorrector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GrammarController : ControllerBase
{
    private readonly IGrammarService _grammarService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<GrammarController> _logger;

    public GrammarController(
        IGrammarService grammarService,
        ISubscriptionService subscriptionService,
        ILogger<GrammarController> logger)
    {
        _grammarService = grammarService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/grammar/check
    /// Accepts user text and returns spelling/grammar corrections.
    /// Requires authentication and checks user quota before processing.
    /// </summary>
    [HttpPost("check")]
    [Authorize]
    [ProducesResponseType(typeof(CorrectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Check(
        [FromBody] CorrectionRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Text))
            return BadRequest(new { error = "Text must not be empty." });

        if (request.Text.Length > 5000)
            return BadRequest(new { error = "Text must be 5,000 characters or fewer." });

        try
        {
            // Get user ID from token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new { error = "Invalid user ID in token." });

            // Check quota
            var isQuotaExceeded = await _subscriptionService.IsQuotaExceededAsync(userId);
            if (isQuotaExceeded)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { error = "You have exceeded your monthly correction quota. Upgrade to Unlimited to continue." });
            }

            // Process grammar check
            var result = await _grammarService.CheckTextAsync(request, ct);

            // Increment usage metrics
            var errorCount = result.Matches?.Count ?? 0;
            await _subscriptionService.IncrementUsageAsync(userId, request.Text.Length, errorCount);

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout,
                new { error = "The request timed out. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during grammar check.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred. Please try again." });
        }
    }

    /// <summary>
    /// GET /api/grammar/health
    /// Simple liveness probe used by the frontend to verify the API is up.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
