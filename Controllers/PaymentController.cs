using GrammarCorrector.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GrammarCorrector.Controllers;

/// <summary>
/// Payment history controller for viewing transactions.
/// </summary>
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
    /// Gets payment history for the authenticated user.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<PaymentHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentHistory()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new { error = "Invalid user ID in token." });

            var history = await _paymentService.GetPaymentHistoryAsync(userId);

            var response = history.Select(p => new PaymentHistoryResponse
            {
                Id = p.Id,
                StripePaymentIntentId = p.StripePaymentIntentId,
                AmountInCents = p.AmountInCents,
                AmountFormatted = $"${(p.AmountInCents / 100m):F2}",
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
}

/// <summary>
/// Response DTOs
/// </summary>
public class PaymentHistoryResponse
{
    public int Id { get; set; }
    public string StripePaymentIntentId { get; set; } = null!;
    public decimal AmountInCents { get; set; }
    public string AmountFormatted { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string SubscriptionTier { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public string? InvoiceReference { get; set; }
}
