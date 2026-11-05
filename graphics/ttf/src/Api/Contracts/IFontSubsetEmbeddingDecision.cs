namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontSubsetEmbeddingDecision"/>.</summary>
public interface IFontSubsetEmbeddingDecision
{
    /// <summary>Reports whether an OS/2 embedding-rights record was available.</summary>
    /// <returns>True when rights information was available.</returns>
    bool HasRightsInformation();

    /// <summary>Gets the interpreted source rights.</summary>
    /// <returns>The rights report, or null when OS/2 rights were unavailable.</returns>
    FontEmbeddingRights? GetRights();

    /// <summary>Gets the embedding mode requested by the caller.</summary>
    /// <returns>The requested mode.</returns>
    FontSubsetEmbeddingMode GetRequestedEmbeddingMode();

    /// <summary>Gets the embedding mode selected after applying rights policy.</summary>
    /// <returns>The effective mode.</returns>
    FontSubsetEmbeddingMode GetEffectiveEmbeddingMode();

    /// <summary>Gets the policy used to resolve rights conflicts.</summary>
    /// <returns>The selected rights policy.</returns>
    FontSubsetRightsPolicy GetPolicy();

    /// <summary>Reports whether the requested output conflicts with source rights.</summary>
    /// <returns>True when the request conflicts with source rights.</returns>
    bool HasPolicyConflict();

    /// <summary>Reports whether a conflict was returned without changing output.</summary>
    /// <returns>True for a report-only conflict.</returns>
    bool WasConflictReported();

    /// <summary>Reports whether no-subsetting rights forced complete-font output.</summary>
    /// <returns>True when subset output was changed to full-font output.</returns>
    bool WasFullFontForced();

    /// <summary>Gets the number of active rights diagnostics.</summary>
    /// <returns>The diagnostic count.</returns>
    int GetDiagnosticCount();

    /// <summary>Gets one active rights diagnostic.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the diagnostic list.
    /// </exception>
    FontDiagnostic GetDiagnostic(int index);
}
