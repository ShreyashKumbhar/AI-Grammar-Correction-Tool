namespace GrammarCorrector.Models;

/// <summary>
/// Represents subscription tier configurations.
/// </summary>
public class Subscription
{
    public int Id { get; set; }

    /// <summary>
    /// Subscription tier name (Free or Unlimited).
    /// </summary>
    public SubscriptionTier Tier { get; set; }

    /// <summary>
    /// Monthly correction quota. Null means unlimited.
    /// </summary>
    public int? MonthlyQuota { get; set; }

    /// <summary>
    /// Monthly subscription price in INR (0 for Free tier).
    /// </summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>
    /// Description of the subscription tier.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Optional Razorpay plan ID for recurring billing.
    /// </summary>
    public string? RazorpayPlanId { get; set; }

    /// <summary>
    /// Flag indicating if this tier is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date when this subscription configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when this subscription configuration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
