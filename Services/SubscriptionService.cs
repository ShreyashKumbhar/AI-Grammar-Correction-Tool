using GrammarCorrector.Data;
using GrammarCorrector.Models;
using Microsoft.EntityFrameworkCore;

namespace GrammarCorrector.Services;

/// <summary>
/// Subscription and quota management service.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(ApplicationDbContext db, ILogger<SubscriptionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Gets the subscription tier for a user.
    /// </summary>
    public async Task<SubscriptionTier> GetUserSubscriptionTierAsync(int userId)
    {
        try
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return SubscriptionTier.Free;

            // Check if subscription has expired
            if (user.SubscriptionTier == SubscriptionTier.Unlimited &&
                user.SubscriptionEndDate.HasValue &&
                user.SubscriptionEndDate < DateTime.UtcNow)
            {
                // Auto-downgrade expired subscription
                user.SubscriptionTier = SubscriptionTier.Free;
                user.SubscriptionEndDate = null;
                await _db.SaveChangesAsync();
            }

            return user.SubscriptionTier;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting subscription tier for user {userId}");
            return SubscriptionTier.Free;
        }
    }

    /// <summary>
    /// Gets current month usage metrics for a user.
    /// </summary>
    public async Task<UsageMetrics?> GetCurrentMonthUsageAsync(int userId)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _db.UsageMetrics
                .FirstOrDefaultAsync(u =>
                    u.UserId == userId &&
                    u.Year == now.Year &&
                    u.Month == now.Month);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting usage metrics for user {userId}");
            return null;
        }
    }

    /// <summary>
    /// Increments usage counters for a user.
    /// </summary>
    public async Task IncrementUsageAsync(int userId, int characterCount, int errorCount)
    {
        try
        {
            var now = DateTime.UtcNow;
            var usage = await GetCurrentMonthUsageAsync(userId);

            if (usage == null)
            {
                // Create new usage record for this month
                var tier = await GetUserSubscriptionTierAsync(userId);
                var quota = tier == SubscriptionTier.Free ? 500 : (int?)null;

                usage = new UsageMetrics
                {
                    UserId = userId,
                    Year = now.Year,
                    Month = now.Month,
                    CorrectionCount = 1,
                    TotalCharactersProcessed = characterCount,
                    TotalErrorsDetected = errorCount,
                    QuotaLimit = quota,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.UsageMetrics.Add(usage);
            }
            else
            {
                usage.CorrectionCount++;
                usage.TotalCharactersProcessed += characterCount;
                usage.TotalErrorsDetected += errorCount;
                usage.UpdatedAt = DateTime.UtcNow;
                _db.UsageMetrics.Update(usage);
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error incrementing usage for user {userId}");
        }
    }

    /// <summary>
    /// Checks if user has exceeded their quota.
    /// </summary>
    public async Task<bool> IsQuotaExceededAsync(int userId)
    {
        try
        {
            var tier = await GetUserSubscriptionTierAsync(userId);

            // Unlimited tier has no quota
            if (tier == SubscriptionTier.Unlimited)
                return false;

            var usage = await GetCurrentMonthUsageAsync(userId);
            if (usage == null)
                return false;

            // Free tier has 500 corrections per month
            const int freeQuota = 500;
            return usage.CorrectionCount >= freeQuota;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking quota for user {userId}");
            return false;
        }
    }

    /// <summary>
    /// Gets remaining quota for current month.
    /// </summary>
    public async Task<int> GetRemainingQuotaAsync(int userId)
    {
        try
        {
            var tier = await GetUserSubscriptionTierAsync(userId);

            // Unlimited tier
            if (tier == SubscriptionTier.Unlimited)
                return -1;

            var usage = await GetCurrentMonthUsageAsync(userId);
            if (usage == null)
                return 500; // Free tier quota

            const int freeQuota = 500;
            var remaining = freeQuota - usage.CorrectionCount;
            return remaining > 0 ? remaining : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting remaining quota for user {userId}");
            return 0;
        }
    }

    /// <summary>
    /// Gets subscription tier details.
    /// </summary>
    public async Task<Subscription?> GetSubscriptionTierDetailsAsync(SubscriptionTier tier)
    {
        try
        {
            return await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.Tier == tier && s.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting subscription tier details for {tier}");
            return null;
        }
    }

    /// <summary>
    /// Gets all available subscription tiers.
    /// </summary>
    public async Task<List<Subscription>> GetAllSubscriptionTiersAsync()
    {
        try
        {
            return await _db.Subscriptions
                .Where(s => s.IsActive)
                .OrderBy(s => s.Tier)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all subscription tiers");
            return new List<Subscription>();
        }
    }

    /// <summary>
    /// Upgrades user subscription to a new tier.
    /// </summary>
    public async Task<bool> UpgradeSubscriptionAsync(int userId, SubscriptionTier newTier, DateTime subscriptionEndDate)
    {
        try
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.SubscriptionTier = newTier;
            user.SubscriptionStartDate = DateTime.UtcNow;
            user.SubscriptionEndDate = subscriptionEndDate;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"User {userId} upgraded to {newTier} tier until {subscriptionEndDate}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error upgrading subscription for user {userId}");
            return false;
        }
    }

    /// <summary>
    /// Downgrades user subscription to Free tier.
    /// </summary>
    public async Task<bool> DowngradeToFreeAsync(int userId)
    {
        try
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.SubscriptionTier = SubscriptionTier.Free;
            user.SubscriptionEndDate = null;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"User {userId} downgraded to Free tier");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downgrading user {userId} to Free tier");
            return false;
        }
    }

    /// <summary>
    /// Gets usage analytics for a user over a date range.
    /// </summary>
    public async Task<UsageAnalytics> GetUsageAnalyticsAsync(int userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var usageRecords = await _db.UsageMetrics
                .Where(u =>
                    u.UserId == userId &&
                    (u.Year > startDate.Year || (u.Year == startDate.Year && u.Month >= startDate.Month)) &&
                    (u.Year < endDate.Year || (u.Year == endDate.Year && u.Month <= endDate.Month)))
                .ToListAsync();

            var analytics = new UsageAnalytics
            {
                TotalCorrections = usageRecords.Sum(u => u.CorrectionCount),
                TotalCharactersProcessed = usageRecords.Sum(u => u.TotalCharactersProcessed),
                TotalErrorsDetected = usageRecords.Sum(u => u.TotalErrorsDetected),
                AverageErrorsPerCorrection = usageRecords.Count > 0
                    ? (double)usageRecords.Sum(u => u.TotalErrorsDetected) / usageRecords.Sum(u => u.CorrectionCount)
                    : 0,
                MonthlyBreakdown = usageRecords
                    .GroupBy(u => $"{u.Year}-{u.Month:D2}")
                    .ToDictionary(g => g.Key, g => g.Sum(u => u.CorrectionCount))
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting usage analytics for user {userId}");
            return new UsageAnalytics();
        }
    }
}
