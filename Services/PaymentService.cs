using GrammarCorrector.Data;
using GrammarCorrector.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GrammarCorrector.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _authService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly RazorpayApiClient _razorpay;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ApplicationDbContext db,
        IAuthService authService,
        ISubscriptionService subscriptionService,
        RazorpayApiClient razorpay,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _authService = authService;
        _subscriptionService = subscriptionService;
        _razorpay = razorpay;
        _logger = logger;
    }

    public async Task<PaymentOrderResult> CreatePaymentOrderAsync(int userId, SubscriptionTier tier)
    {
        try
        {
            if (!_razorpay.IsConfigured)
            {
                return new PaymentOrderResult
                {
                    Success = false,
                    Error = "Payment gateway is not configured. Add Razorpay keys in appsettings."
                };
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return new PaymentOrderResult { Success = false, Error = "User not found." };

            var tierDetails = await _subscriptionService.GetSubscriptionTierDetailsAsync(tier);
            if (tierDetails == null)
                return new PaymentOrderResult { Success = false, Error = "Subscription tier not found." };

            if (tierDetails.MonthlyPrice <= 0)
                return new PaymentOrderResult { Success = false, Error = "Invalid subscription price." };

            var amountInPaise = (long)(tierDetails.MonthlyPrice * 100);
            var receipt = $"prose_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var notes = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "subscriptionTier", tier.ToString() }
            };

            var order = await _razorpay.CreateOrderAsync(amountInPaise, receipt, notes);
            if (order == null)
            {
                return new PaymentOrderResult
                {
                    Success = false,
                    Error = "Could not create Razorpay order. Check your API keys."
                };
            }

            _logger.LogInformation("Razorpay order {OrderId} created for user {UserId}", order.Id, userId);

            return new PaymentOrderResult
            {
                Success = true,
                OrderId = order.Id,
                KeyId = _razorpay.KeyId,
                AmountInPaise = amountInPaise,
                Currency = "INR"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Razorpay order for user {UserId}", userId);
            return new PaymentOrderResult
            {
                Success = false,
                Error = "An error occurred while starting payment."
            };
        }
    }

    public async Task<bool> VerifyAndCompletePaymentAsync(
        int userId,
        string orderId,
        string paymentId,
        string signature)
    {
        if (!_razorpay.VerifyCheckoutSignature(orderId, paymentId, signature))
        {
            _logger.LogWarning("Invalid Razorpay signature for user {UserId}", userId);
            return false;
        }

        return await CompleteSubscriptionUpgradeAsync(userId, paymentId, orderId);
    }

    public async Task<bool> CompleteSubscriptionUpgradeAsync(int userId, string paymentId, string? orderId = null)
    {
        try
        {
            var existing = await _db.Payments
                .AnyAsync(p => p.RazorpayPaymentId == paymentId && p.Status == PaymentStatus.Completed);
            if (existing)
                return true;

            var payment = await _razorpay.GetPaymentAsync(paymentId);
            if (payment == null || payment.Status != "captured")
                return false;

            if (!string.IsNullOrEmpty(orderId) && payment.OrderId != orderId)
                return false;

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return false;

            var tier = SubscriptionTier.Unlimited;
            var subscriptionEndDate = DateTime.UtcNow.AddMonths(1);
            var upgradeSuccess = await _subscriptionService.UpgradeSubscriptionAsync(userId, tier, subscriptionEndDate);
            if (!upgradeSuccess)
                return false;

            var record = new Payment
            {
                UserId = userId,
                RazorpayPaymentId = paymentId,
                AmountInPaise = payment.Amount,
                Status = PaymentStatus.Completed,
                SubscriptionTier = tier,
                PaymentDate = DateTime.UtcNow,
                InvoiceReference = payment.OrderId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Payments.Add(record);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Subscription upgrade completed for user {UserId} via Razorpay {PaymentId}",
                userId, paymentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing subscription upgrade for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> CancelSubscriptionAsync(int userId)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return false;

            user.StripeSubscriptionId = null;
            user.SubscriptionEndDate = null;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Subscription cancelled locally for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", userId);
            return false;
        }
    }

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
                RazorpayPaymentId = p.RazorpayPaymentId,
                AmountInPaise = p.AmountInPaise,
                Status = p.Status.ToString(),
                SubscriptionTier = p.SubscriptionTier.ToString(),
                PaymentDate = p.PaymentDate,
                InvoiceReference = p.InvoiceReference
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment history for user {UserId}", userId);
            return new List<PaymentHistory>();
        }
    }

    public async Task<bool> HandleWebhookAsync(string json, string signatureHeader)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(signatureHeader) ||
                !_razorpay.VerifyWebhookSignature(json, signatureHeader))
            {
                _logger.LogWarning("Razorpay webhook signature verification failed");
                return false;
            }

            var payload = JsonSerializer.Deserialize<RazorpayWebhookPayload>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (payload?.Payload?.Payment?.Entity == null)
                return true;

            var payment = payload.Payload.Payment.Entity;

            if (!payment.Id.StartsWith("pay_", StringComparison.Ordinal))
                return true;

            switch (payload.Event)
            {
                case "payment.captured":
                    return await HandlePaymentCaptured(payment);

                case "payment.failed":
                    return await HandlePaymentFailed(payment);

                default:
                    _logger.LogInformation("Unhandled Razorpay event: {Event}", payload.Event);
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Razorpay webhook");
            return false;
        }
    }

    private async Task<bool> HandlePaymentCaptured(RazorpayPayment payment)
    {
        var order = await _db.Payments.FirstOrDefaultAsync(p => p.InvoiceReference == payment.OrderId);
        // Resolve userId from order notes via Razorpay order - fetch payment notes not stored; use order receipt pattern
        // Webhook payment may include notes in entity - for simplicity fetch order from API not implemented
        // Try parse from existing pending payment or skip if verify endpoint already handled

        var userId = await ResolveUserIdFromOrderAsync(payment.OrderId);
        if (userId == null)
        {
            _logger.LogWarning("Could not resolve user for Razorpay order {OrderId}", payment.OrderId);
            return true;
        }

        return await CompleteSubscriptionUpgradeAsync(userId.Value, payment.Id, payment.OrderId);
    }

    private async Task<bool> HandlePaymentFailed(RazorpayPayment payment)
    {
        var userId = await ResolveUserIdFromOrderAsync(payment.OrderId);
        if (userId == null)
            return true;

        var exists = await _db.Payments.AnyAsync(p => p.RazorpayPaymentId == payment.Id);
        if (exists)
            return true;

        _db.Payments.Add(new Payment
        {
            UserId = userId.Value,
            RazorpayPaymentId = payment.Id,
            AmountInPaise = payment.Amount,
            Status = PaymentStatus.Failed,
            SubscriptionTier = SubscriptionTier.Unlimited,
            PaymentDate = DateTime.UtcNow,
            ErrorMessage = payment.ErrorDescription,
            InvoiceReference = payment.OrderId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<int?> ResolveUserIdFromOrderAsync(string orderId)
    {
        var existing = await _db.Payments
            .Where(p => p.InvoiceReference == orderId)
            .Select(p => (int?)p.UserId)
            .FirstOrDefaultAsync();

        if (existing != null)
            return existing;

        var order = await _razorpay.GetOrderAsync(orderId);
        if (order?.Notes != null &&
            order.Notes.TryGetValue("userId", out var userIdStr) &&
            int.TryParse(userIdStr, out var userIdFromNotes))
        {
            return userIdFromNotes;
        }

        if (!string.IsNullOrEmpty(order?.Receipt))
        {
            var parts = order.Receipt.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[0] == "prose" && int.TryParse(parts[1], out var userIdFromReceipt))
                return userIdFromReceipt;
        }

        return null;
    }
}
