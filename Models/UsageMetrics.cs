namespace GrammarCorrector.Models;

/// <summary>
/// Tracks usage metrics for a user in a specific month.
/// </summary>
public class UsageMetrics
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
    /// Year of the usage period.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month of the usage period (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Number of corrections performed in this month.
    /// </summary>
    public int CorrectionCount { get; set; }

    /// <summary>
    /// Total characters processed in this month.
    /// </summary>
    public long TotalCharactersProcessed { get; set; }

    /// <summary>
    /// Total errors detected in this month.
    /// </summary>
    public int TotalErrorsDetected { get; set; }

    /// <summary>
    /// Quota limit for this month (null if unlimited).
    /// </summary>
    public int? QuotaLimit { get; set; }

    /// <summary>
    /// Date when this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
