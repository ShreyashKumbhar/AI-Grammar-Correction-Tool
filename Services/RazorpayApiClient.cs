using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace GrammarCorrector.Services;

public class RazorpayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly RazorpayOptions _options;
    private readonly ILogger<RazorpayApiClient> _logger;

    public RazorpayApiClient(
        HttpClient http,
        IOptions<RazorpayOptions> options,
        ILogger<RazorpayApiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.KeyId}:{_options.KeySecret}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        _http.BaseAddress = new Uri("https://api.razorpay.com/v1/");
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.KeyId) &&
        !string.IsNullOrWhiteSpace(_options.KeySecret);

    public string KeyId => _options.KeyId;

    public async Task<RazorpayOrder?> CreateOrderAsync(
        long amountInPaise,
        string receipt,
        Dictionary<string, string> notes,
        CancellationToken ct = default)
    {
        var body = new
        {
            amount = amountInPaise,
            currency = "INR",
            receipt,
            notes
        };

        using var response = await _http.PostAsJsonAsync("orders", body, JsonOptions, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay order creation failed ({Status}): {Body}",
                response.StatusCode, content);
            return null;
        }

        return JsonSerializer.Deserialize<RazorpayOrder>(content, JsonOptions);
    }

    public async Task<RazorpayOrderDetails?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"orders/{orderId}", ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay order fetch failed ({Status}): {Body}",
                response.StatusCode, content);
            return null;
        }

        return JsonSerializer.Deserialize<RazorpayOrderDetails>(content, JsonOptions);
    }

    public async Task<RazorpayPayment?> GetPaymentAsync(string paymentId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"payments/{paymentId}", ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay payment fetch failed ({Status}): {Body}",
                response.StatusCode, content);
            return null;
        }

        return JsonSerializer.Deserialize<RazorpayPayment>(content, JsonOptions);
    }

    public bool VerifyCheckoutSignature(string orderId, string paymentId, string signature)
    {
        if (string.IsNullOrWhiteSpace(_options.KeySecret))
            return false;

        var payload = $"{orderId}|{paymentId}";
        var expected = ComputeHmacSha256(payload, _options.KeySecret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }

    public bool VerifyWebhookSignature(string body, string signature)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            return false;

        var expected = ComputeHmacSha256(body, _options.WebhookSecret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }

    private static string ComputeHmacSha256(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public class RazorpayOrder
{
    public string Id { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = string.Empty;
}

public class RazorpayOrderDetails : RazorpayOrder
{
    public string Receipt { get; set; } = string.Empty;
    public Dictionary<string, string>? Notes { get; set; }
}

public class RazorpayPayment
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = string.Empty;
    public string? ErrorDescription { get; set; }
}

public class RazorpayWebhookPayload
{
    public string Event { get; set; } = string.Empty;
    public RazorpayWebhookPayloadData? Payload { get; set; }
}

public class RazorpayWebhookPayloadData
{
    public RazorpayWebhookEntity? Payment { get; set; }
}

public class RazorpayWebhookEntity
{
    public RazorpayPayment? Entity { get; set; }
}
