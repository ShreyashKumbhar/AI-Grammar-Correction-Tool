using GrammarCorrector.Models;

namespace GrammarCorrector.Services;

/// <summary>
/// Interface for subscription and quota management.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Gets the subscription tier for a user.
    /// </summary>
    Task<SubscriptionTier> GetUserSubscriptionTierAsync(int userId);

    /// <summary>
    /// Gets monthly usage metrics for a user.
    /// </summary>
    Task<UsageMetrics?> GetCurrentMonthUsageAsync(int userId);

    /// <summary>
    /// Increments correction count and characters processed for a user.
    /// </summary>
    Task IncrementUsageAsync(int userId, int characterCount, int errorCount);

    /// <summary>
    /// Checks if user has reached their quota for the current month.
    /// </summary>
    Task<bool> IsQuotaExceededAsync(int userId);

    /// <summary>
    /// Gets remaining quota for current month (-1 if unlimited).
    /// </summary>
    Task<int> GetRemainingQuotaAsync(int userId);

    /// <summary>
    /// Gets subscription tier details.
    /// </summary>
    Task<Subscription?> GetSubscriptionTierDetailsAsync(SubscriptionTier tier);

    /// <summary>
    /// Gets all available subscription tiers.
    /// </summary>
    Task<List<Subscription>> GetAllSubscriptionTiersAsync();

    /// <summary>
    /// Upgrades user to a new subscription tier.
    /// </summary>
    Task<bool> UpgradeSubscriptionAsync(int userId, SubscriptionTier newTier, DateTime subscriptionEndDate);

    /// <summary>
    /// Downgrades user subscription to Free tier.
    /// </summary>
    Task<bool> DowngradeToFreeAsync(int userId);

    /// <summary>
    /// Gets usage analytics for a user over a date range.
    /// </summary>
    Task<UsageAnalytics> GetUsageAnalyticsAsync(int userId, DateTime startDate, DateTime endDate);
}

/// <summary>
/// Usage analytics summary.
/// </summary>
public class UsageAnalytics
{
    public int TotalCorrections { get; set; }
    public long TotalCharactersProcessed { get; set; }
    public int TotalErrorsDetected { get; set; }
    public double AverageErrorsPerCorrection { get; set; }
    public Dictionary<string, int>? MonthlyBreakdown { get; set; }
}

/// <summary>
/// Quota status information.
/// </summary>
public class QuotaStatus
{
    public bool IsUnlimited { get; set; }
    public int Used { get; set; }
    public int? Limit { get; set; }
    public int Remaining { get; set; }
    public double PercentageUsed { get; set; }
}
