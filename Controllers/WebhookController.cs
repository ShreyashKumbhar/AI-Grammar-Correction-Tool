using GrammarCorrector.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrammarCorrector.Controllers;

/// <summary>
/// Webhook controller for Razorpay payment events.
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
    /// POST /api/webhook/razorpay
    /// </summary>
    [HttpPost("razorpay")]
    public async Task<IActionResult> HandleRazorpayWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signatureHeader = Request.Headers["X-Razorpay-Signature"].ToString();

            var result = await _paymentService.HandleWebhookAsync(json, signatureHeader);

            if (!result)
            {
                _logger.LogWarning("Failed to process Razorpay webhook");
                return BadRequest(new { error = "Failed to process webhook" });
            }

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Razorpay webhook");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while processing the webhook" });
        }
    }
}
