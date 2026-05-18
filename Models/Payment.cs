namespace GrammarCorrector.Models;

/// <summary>
/// Represents a payment transaction.
/// </summary>
public class Payment
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the User.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Navigation property to User.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Unique Stripe payment intent ID.
    /// </summary>
    public string StripePaymentIntentId { get; set; } = null!;

    /// <summary>
    /// Amount paid in USD cents.
    /// </summary>
    public decimal AmountInCents { get; set; }

    /// <summary>
    /// Payment status (Pending, Completed, Failed, Refunded).
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Subscription tier purchased.
    /// </summary>
    public SubscriptionTier SubscriptionTier { get; set; }

    /// <summary>
    /// Date of the payment.
    /// </summary>
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Invoice number or reference.
    /// </summary>
    public string? InvoiceReference { get; set; }

    /// <summary>
    /// Error message if payment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Date when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Enum for payment statuses.
/// </summary>
public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}
