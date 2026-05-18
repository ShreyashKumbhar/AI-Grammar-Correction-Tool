using GrammarCorrector.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrammarCorrector.Controllers;

/// <summary>
/// Webhook controller for Stripe payment events.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IPaymentService paymentService, ILogger<WebhookController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/webhook/stripe
    /// Receives Stripe webhook events for payment processing.
    /// </summary>
    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signatureHeader = Request.Headers["Stripe-Signature"];

            var result = await _paymentService.HandleWebhookAsync(json, signatureHeader);

            if (!result)
            {
                _logger.LogWarning("Failed to process Stripe webhook");
                return BadRequest(new { error = "Failed to process webhook" });
            }

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe webhook");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while processing the webhook" });
        }
    }
}
