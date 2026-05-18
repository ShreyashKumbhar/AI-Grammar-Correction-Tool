using GrammarCorrector.Models;
using Stripe;

namespace GrammarCorrector.Services;

/// <summary>
/// Interface for payment processing.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a Stripe payment intent for subscription upgrade.
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(int userId, SubscriptionTier tier);

    /// <summary>
    /// Completes subscription upgrade after successful payment.
    /// </summary>
    Task<bool> CompleteSubscriptionUpgradeAsync(int userId, string paymentIntentId);

    /// <summary>
    /// Cancels a user's active subscription.
    /// </summary>
    Task<bool> CancelSubscriptionAsync(int userId);

    /// <summary>
    /// Gets payment history for a user.
    /// </summary>
    Task<List<PaymentHistory>> GetPaymentHistoryAsync(int userId);

    /// <summary>
    /// Handles Stripe webhook for payment events.
    /// </summary>
    Task<bool> HandleWebhookAsync(string json, string signatureHeader);
}

/// <summary>
/// Result of payment intent creation.
/// </summary>
public class PaymentIntentResult
{
    public bool Success { get; set; }
    public string? ClientSecret { get; set; }
    public decimal AmountInCents { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Payment history entry.
/// </summary>
public class PaymentHistory
{
    public int Id { get; set; }
    public string StripePaymentIntentId { get; set; } = null!;
    public decimal AmountInCents { get; set; }
    public string Status { get; set; } = null!;
    public string SubscriptionTier { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public string? InvoiceReference { get; set; }
}
