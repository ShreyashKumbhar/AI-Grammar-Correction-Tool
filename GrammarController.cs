using GrammarCorrector.Models;
using GrammarCorrector.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrammarCorrector.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GrammarController : ControllerBase
{
    private readonly IGrammarService _grammarService;
    private readonly ILogger<GrammarController> _logger;

    public GrammarController(IGrammarService grammarService, ILogger<GrammarController> logger)
    {
        _grammarService = grammarService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/grammar/check
    /// Accepts user text and returns spelling/grammar corrections.
    /// </summary>
    [HttpPost("check")]
    [ProducesResponseType(typeof(CorrectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            var result = await _grammarService.CheckTextAsync(request, ct);
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
