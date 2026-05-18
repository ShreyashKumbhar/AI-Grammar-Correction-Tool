using GrammarCorrector.Models;

namespace GrammarCorrector.Services;

public interface IGrammarService
{
    /// <summary>
    /// Analyses <paramref name="request"/> and returns all detected
    /// spelling/grammar issues together with the auto-corrected text.
    /// </summary>
    Task<CorrectionResponse> CheckTextAsync(CorrectionRequest request, CancellationToken ct = default);
}
