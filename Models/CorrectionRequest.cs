namespace GrammarCorrector.Models;

/// <summary>
/// Incoming request payload from the frontend.
/// </summary>
public class CorrectionRequest
{
    /// <summary>The raw user-submitted text to analyse.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>BCP-47 language tag, e.g. "en-US".</summary>
    public string Language { get; set; } = "en-US";
}
