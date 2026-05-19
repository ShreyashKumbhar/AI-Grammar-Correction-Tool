using GrammarCorrector.Models;

namespace GrammarCorrector.Services;

public interface IPaymentService
{
    Task<PaymentOrderResult> CreatePaymentOrderAsync(int userId, SubscriptionTier tier);

    Task<bool> VerifyAndCompletePaymentAsync(
        int userId,
        string orderId,
        string paymentId,
        string signature);

    Task<bool> CancelSubscriptionAsync(int userId);

    Task<List<PaymentHistory>> GetPaymentHistoryAsync(int userId);

    Task<bool> HandleWebhookAsync(string json, string signatureHeader);
}

public class PaymentOrderResult
{
    public bool Success { get; set; }
    public string? OrderId { get; set; }
    public string? KeyId { get; set; }
    public long AmountInPaise { get; set; }
    public string Currency { get; set; } = "INR";
    public string? Error { get; set; }
}

public class PaymentHistory
{
    public int Id { get; set; }
    public string RazorpayPaymentId { get; set; } = null!;
    public decimal AmountInPaise { get; set; }
    public string Status { get; set; } = null!;
    public string SubscriptionTier { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public string? InvoiceReference { get; set; }
}
