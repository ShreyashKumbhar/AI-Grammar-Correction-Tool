using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GrammarCorrector.Models;

namespace GrammarCorrector.Services;

/// <summary>
/// Calls the public LanguageTool REST API (https://api.languagetool.org/v2/check)
/// to detect spelling mistakes and grammatical errors.
///
/// If the external API is unreachable the service falls back to a built-in
/// rule-based engine so the app keeps working without internet access.
/// </summary>
public class GrammarService : IGrammarService
{
    private readonly HttpClient _http;
    private readonly ILogger<GrammarService> _logger;
    private readonly IConfiguration _config;

    // JSON options shared across all deserialization calls
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GrammarService(HttpClient http, ILogger<GrammarService> logger, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _config = config;
    }

    // -----------------------------------------------------------------------
    // Public entry point
    // -----------------------------------------------------------------------

    public async Task<CorrectionResponse> CheckTextAsync(CorrectionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return new CorrectionResponse
            {
                OriginalText = request.Text,
                CorrectedText = request.Text
            };
        }

        List<LanguageMatch> matches;

        try
        {
            matches = await CheckWithLanguageToolAsync(request.Text, request.Language, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LanguageTool API unavailable – falling back to built-in rules.");
            matches = FallbackRuleEngine(request.Text);
        }

        // Build auto-corrected text by applying the best suggestion per match
        var correctedText = ApplyCorrections(request.Text, matches);

        return new CorrectionResponse
        {
            OriginalText = request.Text,
            CorrectedText = correctedText,
            Matches = matches,
            SpellingErrorCount = matches.Count(m => m.IssueType == "spelling"),
            GrammarErrorCount  = matches.Count(m => m.IssueType == "grammar"),
            StyleIssueCount    = matches.Count(m => m.IssueType == "style")
        };
    }

    // -----------------------------------------------------------------------
    // LanguageTool API call
    // -----------------------------------------------------------------------

    private async Task<List<LanguageMatch>> CheckWithLanguageToolAsync(
        string text, string language, CancellationToken ct)
    {
        var endpoint = _config["LanguageTool:Endpoint"] ?? "/v2/check";

        // LanguageTool expects application/x-www-form-urlencoded
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["text"]     = text,
            ["language"] = language,
            ["enabledOnly"] = "false"
        });

        var response = await _http.PostAsync(endpoint, form, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var ltResponse = JsonSerializer.Deserialize<LanguageToolResponse>(json, _jsonOpts)
                         ?? new LanguageToolResponse();

        return ltResponse.Matches.Select(m => MapMatch(text, m)).ToList();
    }

    private static LanguageMatch MapMatch(string text, LTMatch m)
    {
        // Determine high-level issue type from LanguageTool's category / issueType fields
        var issueType = DetermineIssueType(m.Rule.IssueType, m.Rule.Category.Id, m.Rule.Id);

        return new LanguageMatch
        {
            Offset       = m.Offset,
            Length       = m.Length,
            OriginalText = text.Length >= m.Offset + m.Length
                               ? text.Substring(m.Offset, m.Length)
                               : string.Empty,
            Message      = m.Message,
            ShortMessage = string.IsNullOrWhiteSpace(m.ShortMessage) ? m.Message : m.ShortMessage,
            Suggestions  = m.Replacements.Take(5).Select(r => r.Value).ToList(),
            IssueType    = issueType,
            RuleId       = m.Rule.Id,
            Category     = m.Rule.Category.Name
        };
    }

    private static string DetermineIssueType(string ltIssueType, string categoryId, string ruleId)
    {
        if (!string.IsNullOrEmpty(ltIssueType))
        {
            return ltIssueType.ToLowerInvariant() switch
            {
                "misspelling" or "non-conformance" => "spelling",
                "style"                            => "style",
                _                                  => "grammar"
            };
        }

        // Fallback: infer from category/rule IDs
        if (categoryId is "TYPOS" || ruleId.Contains("MORFOLOGIK") || ruleId.Contains("SPELL"))
            return "spelling";

        if (categoryId is "STYLE" or "REDUNDANCY")
            return "style";

        return "grammar";
    }

    // -----------------------------------------------------------------------
    // Fallback: built-in rule engine (works offline)
    // -----------------------------------------------------------------------

    private static readonly Dictionary<string, string> CommonSpellingErrors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["thier"]       = "their",
        ["recieve"]     = "receive",
        ["occured"]     = "occurred",
        ["seperate"]    = "separate",
        ["definately"]  = "definitely",
        ["accomodate"]  = "accommodate",
        ["begining"]    = "beginning",
        ["beleive"]     = "believe",
        ["calender"]    = "calendar",
        ["cemetary"]    = "cemetery",
        ["concious"]    = "conscious",
        ["dissapear"]   = "disappear",
        ["embarass"]    = "embarrass",
        ["explaination"]= "explanation",
        ["foriegn"]     = "foreign",
        ["goverment"]   = "government",
        ["grammer"]     = "grammar",
        ["harrass"]     = "harass",
        ["independant"] = "independent",
        ["lisence"]     = "license",
        ["maintainance"]= "maintenance",
        ["millenium"]   = "millennium",
        ["mischievous"] = "mischievous",
        ["neccessary"]  = "necessary",
        ["occassion"]   = "occasion",
        ["persistant"]  = "persistent",
        ["privelege"]   = "privilege",
        ["relevent"]    = "relevant",
        ["restaraunt"]  = "restaurant",
        ["rythm"]       = "rhythm",
        ["sieze"]       = "seize",
        ["tommorrow"]   = "tomorrow",
        ["tounge"]      = "tongue",
        ["truely"]      = "truly",
        ["untill"]      = "until",
        ["wierd"]       = "weird"
    };

    // Simple pattern: subject–verb agreement for third-person singular
    private static readonly List<(string Wrong, string Right, string Message)> GrammarRules = new()
    {
        ("she go ",  "she goes ",  "Subject–verb agreement: use 'goes' with 'she'."),
        ("he go ",   "he goes ",   "Subject–verb agreement: use 'goes' with 'he'."),
        ("it go ",   "it goes ",   "Subject–verb agreement: use 'goes' with 'it'."),
        ("she don't","she doesn't","Use 'doesn't' with third-person singular."),
        ("he don't", "he doesn't", "Use 'doesn't' with third-person singular."),
        ("it don't", "it doesn't", "Use 'doesn't' with third-person singular."),
        ("i is ",    "I am ",      "Use 'am' with the first-person singular pronoun 'I'."),
        ("i are ",   "I am ",      "Use 'am' with the first-person singular pronoun 'I'."),
        ("they was ", "they were ","Use 'were' with 'they'."),
        ("we was ",  "we were ",   "Use 'were' with 'we'."),
        ("you was ", "you were ",  "Use 'were' with 'you'."),
        ("a apple",  "an apple",   "Use 'an' before vowel sounds."),
        ("a orange", "an orange",  "Use 'an' before vowel sounds."),
        ("a hour",   "an hour",    "Use 'an' before vowel sounds."),
        ("a honest", "an honest",  "Use 'an' before vowel sounds."),
        ("an university","a university","Use 'a' before consonant sounds (the 'u' in 'university' sounds like 'y')."),
        ("an union", "a union",    "Use 'a' before consonant sounds."),
        ("an european","a european","Use 'a' before consonant sounds."),
    };

    private static List<LanguageMatch> FallbackRuleEngine(string text)
    {
        var matches = new List<LanguageMatch>();
        var lower   = text.ToLowerInvariant();

        // --- Spelling ---
        // Tokenize by splitting on non-word boundaries
        int i = 0;
        while (i < text.Length)
        {
            if (!char.IsLetter(text[i])) { i++; continue; }

            var wordStart = i;
            while (i < text.Length && char.IsLetter(text[i])) i++;
            var word = text.Substring(wordStart, i - wordStart);

            if (CommonSpellingErrors.TryGetValue(word, out var correction))
            {
                matches.Add(new LanguageMatch
                {
                    Offset       = wordStart,
                    Length       = word.Length,
                    OriginalText = word,
                    Message      = $"'{word}' is a common misspelling. Did you mean '{correction}'?",
                    ShortMessage = "Spelling mistake",
                    Suggestions  = new List<string> { CorrectCase(word, correction) },
                    IssueType    = "spelling",
                    RuleId       = "FALLBACK_SPELL",
                    Category     = "Possible Typo"
                });
            }
        }

        // --- Grammar rules ---
        foreach (var (wrong, right, message) in GrammarRules)
        {
            int idx = lower.IndexOf(wrong, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                // Make sure we're not already flagging this span as a spelling error
                var alreadyFlagged = matches.Any(m => m.Offset == idx && m.IssueType == "spelling");
                if (!alreadyFlagged)
                {
                    matches.Add(new LanguageMatch
                    {
                        Offset       = idx,
                        Length       = wrong.TrimEnd().Length,
                        OriginalText = text.Substring(idx, wrong.TrimEnd().Length),
                        Message      = message,
                        ShortMessage = "Grammar issue",
                        Suggestions  = new List<string> { right.TrimEnd() },
                        IssueType    = "grammar",
                        RuleId       = "FALLBACK_GRAMMAR",
                        Category     = "Grammar"
                    });
                }

                idx = lower.IndexOf(wrong, idx + 1, StringComparison.OrdinalIgnoreCase);
            }
        }

        return matches.OrderBy(m => m.Offset).ToList();
    }

    // Preserve the original word's leading capitalisation in the suggestion
    private static string CorrectCase(string original, string correction)
    {
        if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(correction)) return correction;
        return char.IsUpper(original[0])
            ? char.ToUpper(correction[0]) + correction[1..]
            : correction;
    }

    // -----------------------------------------------------------------------
    // Auto-correct: apply best suggestions right-to-left to preserve offsets
    // -----------------------------------------------------------------------

    private static string ApplyCorrections(string text, List<LanguageMatch> matches)
    {
        var sb = new StringBuilder(text);

        // Process from the end so earlier offsets stay valid
        foreach (var match in matches.OrderByDescending(m => m.Offset))
        {
            if (match.Suggestions.Count == 0) continue;
            if (match.Offset < 0 || match.Offset + match.Length > sb.Length) continue;

            sb.Remove(match.Offset, match.Length);
            sb.Insert(match.Offset, match.Suggestions[0]);
        }

        return sb.ToString();
    }
}
