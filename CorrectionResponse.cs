namespace GrammarCorrector.Models;

/// <summary>
/// Full correction result sent back to the client.
/// </summary>
public class CorrectionResponse
{
    public string OriginalText { get; set; } = string.Empty;
    public string CorrectedText { get; set; } = string.Empty;
    public List<LanguageMatch> Matches { get; set; } = new();
    public int SpellingErrorCount { get; set; }
    public int GrammarErrorCount { get; set; }
    public int StyleIssueCount { get; set; }
    public bool HasErrors => Matches.Count > 0;
}

/// <summary>
/// A single error/suggestion match from LanguageTool, mapped to
/// a structure the frontend can consume directly.
/// </summary>
public class LanguageMatch
{
    /// <summary>Character offset in the original string.</summary>
    public int Offset { get; set; }

    /// <summary>Number of characters the erroneous span covers.</summary>
    public int Length { get; set; }

    /// <summary>The original (erroneous) text at this position.</summary>
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>Human-readable explanation of the issue.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>One-line short label (used in the sidebar).</summary>
    public string ShortMessage { get; set; } = string.Empty;

    /// <summary>Ranked list of replacement suggestions.</summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>"spelling" | "grammar" | "style"</summary>
    public string IssueType { get; set; } = "grammar";

    /// <summary>Rule ID from LanguageTool (e.g. "MORFOLOGIK_RULE_EN_US").</summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>Category name (e.g. "Possible Typo").</summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Strongly-typed mirror of the LanguageTool v2/check JSON response.
/// Used internally by GrammarService for deserialization only.
/// </summary>
internal class LanguageToolResponse
{
    public List<LTMatch> Matches { get; set; } = new();
}

internal class LTMatch
{
    public string Message { get; set; } = string.Empty;
    public string ShortMessage { get; set; } = string.Empty;
    public int Offset { get; set; }
    public int Length { get; set; }
    public List<LTReplacement> Replacements { get; set; } = new();
    public LTRule Rule { get; set; } = new();
}

internal class LTReplacement
{
    public string Value { get; set; } = string.Empty;
}

internal class LTRule
{
    public string Id { get; set; } = string.Empty;
    public LTCategory Category { get; set; } = new();
    public string IssueType { get; set; } = string.Empty;
}

internal class LTCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
