using GrammarCorrector.Models;
using GrammarCorrector.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GrammarCorrector.Controllers;

/// <summary>
/// Subscription management controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        IPaymentService paymentService,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/subscription/tiers
    /// Gets all available subscription tiers.
    /// </summary>
    [HttpGet("tiers")]
    [ProducesResponseType(typeof(List<SubscriptionTierResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTiers()
    {
        try
        {
            var tiers = await _subscriptionService.GetAllSubscriptionTiersAsync();
            var response = tiers.Select(t => new SubscriptionTierResponse
            {
                Id = t.Id,
                Tier = t.Tier.ToString(),
                MonthlyQuota = t.MonthlyQuota,
                MonthlyPrice = t.MonthlyPrice,
                Description = t.Description,
                IsActive = t.IsActive
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription tiers");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve subscription tiers." });
        }
    }

    /// <summary>
    /// GET /api/subscription/tier/{tier}
    /// Gets details for a specific subscription tier.
    /// </summary>
    [HttpGet("tier/{tier}")]
    [ProducesResponseType(typeof(SubscriptionTierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTierDetails(string tier)
    {
        try
        {
            if (!Enum.TryParse<SubscriptionTier>(tier, ignoreCase: true, out var tierEnum))
                return BadRequest(new ErrorResponse { Error = "Invalid subscription tier." });

            var tierDetails = await _subscriptionService.GetSubscriptionTierDetailsAsync(tierEnum);
            if (tierDetails == null)
                return NotFound(new ErrorResponse { Error = "Subscription tier not found." });

            var response = new SubscriptionTierResponse
            {
                Id = tierDetails.Id,
                Tier = tierDetails.Tier.ToString(),
                MonthlyQuota = tierDetails.MonthlyQuota,
                MonthlyPrice = tierDetails.MonthlyPrice,
                Description = tierDetails.Description,
                IsActive = tierDetails.IsActive
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tier details");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve tier details." });
        }
    }

    /// <summary>
    /// GET /api/subscription/current
    /// Gets current subscription status for authenticated user.
    /// </summary>
    [HttpGet("current")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new ErrorResponse { Error = "Invalid user ID in token." });

            var tier = await _subscriptionService.GetUserSubscriptionTierAsync(userId);
            var remaining = await _subscriptionService.GetRemainingQuotaAsync(userId);
            var usage = await _subscriptionService.GetCurrentMonthUsageAsync(userId);

            var response = new CurrentSubscriptionResponse
            {
                CurrentTier = tier.ToString(),
                RemainingQuota = remaining,
                IsUnlimited = remaining == -1,
                CurrentMonthUsage = new UsageResponse
                {
                    CorrectionCount = usage?.CorrectionCount ?? 0,
                    TotalCharactersProcessed = usage?.TotalCharactersProcessed ?? 0,
                    TotalErrorsDetected = usage?.TotalErrorsDetected ?? 0
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current subscription");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve subscription status." });
        }
    }

    /// <summary>
    /// GET /api/subscription/quota-status
    /// Gets detailed quota status for authenticated user.
    /// </summary>
    [HttpGet("quota-status")]
    [Authorize]
    [ProducesResponseType(typeof(QuotaStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQuotaStatus()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new ErrorResponse { Error = "Invalid user ID in token." });

            var tier = await _subscriptionService.GetUserSubscriptionTierAsync(userId);
            var usage = await _subscriptionService.GetCurrentMonthUsageAsync(userId);
            var isExceeded = await _subscriptionService.IsQuotaExceededAsync(userId);

            int used = usage?.CorrectionCount ?? 0;
            int? limit = tier == SubscriptionTier.Free ? 500 : null;
            int remaining = await _subscriptionService.GetRemainingQuotaAsync(userId);
            double percentageUsed = limit.HasValue ? (double)used / limit.Value * 100 : 0;

            var response = new QuotaStatusResponse
            {
                IsUnlimited = tier == SubscriptionTier.Unlimited,
                Used = used,
                Limit = limit,
                Remaining = remaining,
                PercentageUsed = percentageUsed,
                IsExceeded = isExceeded
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quota status");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve quota status." });
        }
    }

    /// <summary>
    /// GET /api/subscription/analytics
    /// Gets usage analytics for authenticated user.
    /// </summary>
    [HttpGet("analytics")]
    [Authorize]
    [ProducesResponseType(typeof(AnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new ErrorResponse { Error = "Invalid user ID in token." });

            var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
            var end = endDate ?? DateTime.UtcNow;

            var analytics = await _subscriptionService.GetUsageAnalyticsAsync(userId, start, end);

            var response = new AnalyticsResponse
            {
                TotalCorrections = analytics.TotalCorrections,
                TotalCharactersProcessed = analytics.TotalCharactersProcessed,
                TotalErrorsDetected = analytics.TotalErrorsDetected,
                AverageErrorsPerCorrection = Math.Round(analytics.AverageErrorsPerCorrection, 2),
                MonthlyBreakdown = analytics.MonthlyBreakdown
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to retrieve usage analytics." });
        }
    }

    /// <summary>
    /// POST /api/subscription/upgrade
    /// Initiates upgrade to Unlimited tier (creates payment intent).
    /// </summary>
    [HttpPost("upgrade")]
    [Authorize]
    [ProducesResponseType(typeof(UpgradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InitiateUpgrade()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new ErrorResponse { Error = "Invalid user ID in token." });

            // Create Stripe payment intent
            var result = await _paymentService.CreatePaymentIntentAsync(userId, SubscriptionTier.Unlimited);

            if (!result.Success)
                return BadRequest(new ErrorResponse { Error = result.Error });

            var response = new UpgradeResponse
            {
                ClientSecret = result.ClientSecret,
                Amount = result.AmountInCents,
                Currency = "usd"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating upgrade");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to initiate upgrade." });
        }
    }

    /// <summary>
    /// POST /api/subscription/downgrade
    /// Downgrades user to Free tier and cancels Unlimited subscription.
    /// </summary>
    [HttpPost("downgrade")]
    [Authorize]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Downgrade()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out var userId))
                return Unauthorized(new ErrorResponse { Error = "Invalid user ID in token." });

            var result = await _paymentService.CancelSubscriptionAsync(userId);

            if (!result)
                return BadRequest(new ErrorResponse { Error = "Failed to cancel subscription." });

            await _subscriptionService.DowngradeToFreeAsync(userId);

            return Ok(new SuccessResponse { Message = "Subscription downgraded to Free tier successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downgrading subscription");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "Failed to downgrade subscription." });
        }
    }
}

/// <summary>
/// Response DTOs
/// </summary>
public class SubscriptionTierResponse
{
    public int Id { get; set; }
    public string Tier { get; set; } = null!;
    public int? MonthlyQuota { get; set; }
    public decimal MonthlyPrice { get; set; }
    public string Description { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class CurrentSubscriptionResponse
{
    public string CurrentTier { get; set; } = null!;
    public int RemainingQuota { get; set; }
    public bool IsUnlimited { get; set; }
    public UsageResponse? CurrentMonthUsage { get; set; }
}

public class UsageResponse
{
    public int CorrectionCount { get; set; }
    public long TotalCharactersProcessed { get; set; }
    public int TotalErrorsDetected { get; set; }
}

public class QuotaStatusResponse
{
    public bool IsUnlimited { get; set; }
    public int Used { get; set; }
    public int? Limit { get; set; }
    public int Remaining { get; set; }
    public double PercentageUsed { get; set; }
    public bool IsExceeded { get; set; }
}

public class AnalyticsResponse
{
    public int TotalCorrections { get; set; }
    public long TotalCharactersProcessed { get; set; }
    public int TotalErrorsDetected { get; set; }
    public double AverageErrorsPerCorrection { get; set; }
    public Dictionary<string, int>? MonthlyBreakdown { get; set; }
}

public class UpgradeResponse
{
    public string? ClientSecret { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
}
