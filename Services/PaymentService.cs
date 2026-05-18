using GrammarCorrector.Data;
using GrammarCorrector.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using StripeSubscription = Stripe.Subscription;
using StripeSubscriptionService = Stripe.SubscriptionService;

namespace GrammarCorrector.Services;

/// <summary>
/// Payment processing service with Stripe integration.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IAuthService _authService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext db,
        IConfiguration configuration,
        IAuthService authService,
        ISubscriptionService subscriptionService,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _configuration = configuration;
        _authService = authService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Stripe payment intent for subscription upgrade.
    /// </summary>
    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(int userId, SubscriptionTier tier)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return new PaymentIntentResult { Success = false, Error = "User not found." };

            var tierDetails = await _subscriptionService.GetSubscriptionTierDetailsAsync(tier);
            if (tierDetails == null)
                return new PaymentIntentResult { Success = false, Error = "Subscription tier not found." };

            // Amount should be greater than 0 for payment
            if (tierDetails.MonthlyPrice <= 0)
                return new PaymentIntentResult { Success = false, Error = "Invalid subscription price." };

            // Create or get Stripe customer
            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                var customerOptions = new CustomerCreateOptions
                {
                    Email = user.Email,
                    Name = user.FullName
                };
                var customerService = new CustomerService();
                var customer = await customerService.CreateAsync(customerOptions);
                user.StripeCustomerId = customer.Id;
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
            }

            // Create payment intent
            var amountInCents = (long)(tierDetails.MonthlyPrice * 100);
            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = "usd",
                Customer = user.StripeCustomerId,
                Description = $"Upgrade to {tier} subscription",
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "subscriptionTier", tier.ToString() }
                }
            };

            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

            _logger.LogInformation($"Payment intent created for user {userId}: {paymentIntent.Id}");

            return new PaymentIntentResult
            {
                Success = true,
                ClientSecret = paymentIntent.ClientSecret,
                AmountInCents = amountInCents
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, $"Stripe error creating payment intent for user {userId}");
            return new PaymentIntentResult
            {
                Success = false,
                Error = $"Payment error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating payment intent for user {userId}");
            return new PaymentIntentResult
            {
                Success = false,
                Error = "An error occurred while processing payment."
            };
        }
    }

    /// <summary>
    /// Completes subscription upgrade after successful payment.
    /// </summary>
    public async Task<bool> CompleteSubscriptionUpgradeAsync(int userId, string paymentIntentId)
    {
        try
        {
            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);

            if (paymentIntent.Status != "succeeded")
                return false;

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return false;

            // Parse subscription tier from metadata
            if (!paymentIntent.Metadata.TryGetValue("subscriptionTier", out var tierString) ||
                !Enum.TryParse<SubscriptionTier>(tierString, out var tier))
            {
                return false;
            }

            // Update subscription to Unlimited
            var subscriptionEndDate = DateTime.UtcNow.AddMonths(1);
            var upgradeSuccess = await _subscriptionService.UpgradeSubscriptionAsync(userId, tier, subscriptionEndDate);

            if (!upgradeSuccess)
                return false;

            // Record payment
            var payment = new Payment
            {
                UserId = userId,
                StripePaymentIntentId = paymentIntentId,
                AmountInCents = (decimal)(paymentIntent.Amount),
                Status = PaymentStatus.Completed,
                SubscriptionTier = tier,
                PaymentDate = DateTime.UtcNow,
                InvoiceReference = paymentIntent.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Subscription upgrade completed for user {userId} to {tier} tier");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error completing subscription upgrade for user {userId}");
            return false;
        }
    }

    /// <summary>
    /// Cancels a user's active subscription.
    /// </summary>
    public async Task<bool> CancelSubscriptionAsync(int userId)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return false;

            if (string.IsNullOrEmpty(user.StripeSubscriptionId))
                return true; // No active subscription to cancel

            var stripeSubscriptionService = new StripeSubscriptionService();
            var cancellation = new SubscriptionCancelOptions();
            await stripeSubscriptionService.CancelAsync(user.StripeSubscriptionId, cancellation);

            user.StripeSubscriptionId = null;
            user.SubscriptionEndDate = null;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Subscription cancelled for user {userId}");
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, $"Stripe error cancelling subscription for user {userId}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cancelling subscription for user {userId}");
            return false;
        }
    }

    /// <summary>
    /// Gets payment history for a user.
    /// </summary>
    public async Task<List<PaymentHistory>> GetPaymentHistoryAsync(int userId)
    {
        try
        {
            var payments = await _db.Payments
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(p => new PaymentHistory
            {
                Id = p.Id,
                StripePaymentIntentId = p.StripePaymentIntentId,
                AmountInCents = p.AmountInCents,
                Status = p.Status.ToString(),
                SubscriptionTier = p.SubscriptionTier.ToString(),
                PaymentDate = p.PaymentDate,
                InvoiceReference = p.InvoiceReference
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting payment history for user {userId}");
            return new List<PaymentHistory>();
        }
    }

    /// <summary>
    /// Handles Stripe webhook for payment events.
    /// </summary>
    public async Task<bool> HandleWebhookAsync(string json, string signatureHeader)
    {
        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogWarning("Stripe webhook secret not configured");
                return false;
            }

            var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    return await HandlePaymentSucceeded(stripeEvent.Data.Object as PaymentIntent);

                case "payment_intent.payment_failed":
                    return await HandlePaymentFailed(stripeEvent.Data.Object as PaymentIntent);

                case "customer.subscription.deleted":
                    return await HandleSubscriptionCancelled(stripeEvent.Data.Object as StripeSubscription);

                default:
                    _logger.LogInformation($"Unhandled Stripe event type: {stripeEvent.Type}");
                    return true;
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe webhook");
            return false;
        }
    }

    /// <summary>
    /// Handles payment.intent.succeeded webhook event.
    /// </summary>
    private async Task<bool> HandlePaymentSucceeded(PaymentIntent? paymentIntent)
    {
        if (paymentIntent == null)
            return false;

        if (!paymentIntent.Metadata.TryGetValue("userId", out var userIdStr) ||
            !int.TryParse(userIdStr, out var userId))
        {
            return false;
        }

        return await CompleteSubscriptionUpgradeAsync(userId, paymentIntent.Id);
    }

    /// <summary>
    /// Handles payment.intent.payment_failed webhook event.
    /// </summary>
    private async Task<bool> HandlePaymentFailed(PaymentIntent? paymentIntent)
    {
        if (paymentIntent == null)
            return false;

        try
        {
            if (!paymentIntent.Metadata.TryGetValue("userId", out var userIdStr) ||
                !int.TryParse(userIdStr, out var userId))
            {
                return false;
            }

            var payment = new Payment
            {
                UserId = userId,
                StripePaymentIntentId = paymentIntent.Id,
                AmountInCents = (decimal)(paymentIntent.Amount),
                Status = PaymentStatus.Failed,
                SubscriptionTier = SubscriptionTier.Unlimited,
                PaymentDate = DateTime.UtcNow,
                ErrorMessage = paymentIntent.LastPaymentError?.Message,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            _logger.LogWarning($"Payment failed for user {userId}: {paymentIntent.LastPaymentError?.Message}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment failure");
            return false;
        }
    }

    /// <summary>
    /// Handles customer.subscription.deleted webhook event.
    /// </summary>
    private async Task<bool> HandleSubscriptionCancelled(StripeSubscription? subscription)
    {
        if (subscription == null)
            return false;

        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.StripeSubscriptionId == subscription.Id);
            if (user == null)
                return true;

            user.StripeSubscriptionId = null;
            user.SubscriptionTier = SubscriptionTier.Free;
            user.SubscriptionEndDate = null;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Subscription cancelled via webhook for user {user.Id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription cancellation");
            return false;
        }
    }
}
