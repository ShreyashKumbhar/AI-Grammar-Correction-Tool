using GrammarCorrector.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GrammarCorrector.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/payment/history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<PaymentHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentHistory()
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { error = "Invalid user ID in token." });

            var history = await _paymentService.GetPaymentHistoryAsync(userId.Value);

            var response = history.Select(p => new PaymentHistoryResponse
            {
                Id = p.Id,
                RazorpayPaymentId = p.RazorpayPaymentId,
                AmountInPaise = p.AmountInPaise,
                AmountFormatted = $"₹{(p.AmountInPaise / 100m):N0}",
                Status = p.Status,
                SubscriptionTier = p.SubscriptionTier,
                PaymentDate = p.PaymentDate,
                InvoiceReference = p.InvoiceReference
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to retrieve payment history." });
        }
    }

    /// <summary>
    /// POST /api/payment/verify
    /// Verifies Razorpay checkout signature and completes upgrade.
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RazorpayOrderId) ||
            string.IsNullOrWhiteSpace(request.RazorpayPaymentId) ||
            string.IsNullOrWhiteSpace(request.RazorpaySignature))
        {
            return BadRequest(new { error = "Payment verification data is incomplete." });
        }

        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "Invalid user ID in token." });

        var success = await _paymentService.VerifyAndCompletePaymentAsync(
            userId.Value,
            request.RazorpayOrderId,
            request.RazorpayPaymentId,
            request.RazorpaySignature);

        if (!success)
            return BadRequest(new { error = "Payment verification failed." });

        return Ok(new { message = "Payment successful. Your Unlimited plan is now active." });
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim?.Value, out var userId) ? userId : null;
    }
}

public class VerifyPaymentRequest
{
    public string RazorpayOrderId { get; set; } = null!;
    public string RazorpayPaymentId { get; set; } = null!;
    public string RazorpaySignature { get; set; } = null!;
}

public class PaymentHistoryResponse
{
    public int Id { get; set; }
    public string RazorpayPaymentId { get; set; } = null!;
    public decimal AmountInPaise { get; set; }
    public string AmountFormatted { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string SubscriptionTier { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public string? InvoiceReference { get; set; }
}
