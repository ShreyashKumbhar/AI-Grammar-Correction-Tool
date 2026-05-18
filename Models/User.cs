namespace GrammarCorrector.Models;

/// <summary>
/// Represents a user account with subscription and authentication details.
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// Unique email address for the user.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// User's full name.
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// Hashed password using bcrypt.
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Current subscription tier (Free or Unlimited).
    /// </summary>
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;

    /// <summary>
    /// Date when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the current subscription period started.
    /// </summary>
    public DateTime SubscriptionStartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the current subscription period ends (null for Free tier).
    /// </summary>
    public DateTime? SubscriptionEndDate { get; set; }

    /// <summary>
    /// Stripe customer ID for payment processing.
    /// </summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Current active Stripe subscription ID.
    /// </summary>
    public string? StripeSubscriptionId { get; set; }

    /// <summary>
    /// Flag indicating if the account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<UsageMetrics> UsageHistory { get; set; } = new List<UsageMetrics>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

/// <summary>
/// Enum for subscription tiers.
/// </summary>
public enum SubscriptionTier
{
    Free,
    Unlimited
}
