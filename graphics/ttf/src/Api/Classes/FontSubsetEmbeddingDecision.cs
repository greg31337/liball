namespace ttf;

/// <summary>Reports how embedding rights affected a subset request.</summary>
public class FontSubsetEmbeddingDecision : IFontSubsetEmbeddingDecision
{
    FontEmbeddingRights? rights;
    FontSubsetEmbeddingMode requestedMode;
    FontSubsetEmbeddingMode effectiveMode;
    FontSubsetRightsPolicy policy;
    bool policyConflict;
    bool reportedConflict;
    bool forcedFullFont;
    FontDiagnostic[] diagnostics;

    internal FontSubsetEmbeddingDecision(
        FontEmbeddingRights? rights,
        FontSubsetEmbeddingMode requestedMode,
        FontSubsetEmbeddingMode effectiveMode,
        FontSubsetRightsPolicy policy,
        bool policyConflict,
        bool reportedConflict,
        bool forcedFullFont,
        FontDiagnostic[] diagnostics)
    {
        this.rights = rights;
        this.requestedMode = requestedMode;
        this.effectiveMode = effectiveMode;
        this.policy = policy;
        this.policyConflict = policyConflict;
        this.reportedConflict = reportedConflict;
        this.forcedFullFont = forcedFullFont;
        this.diagnostics = CopyDiagnostics(diagnostics);
    }

    /// <summary>Reports whether an OS/2 embedding-rights record was available.</summary>
    /// <returns>True when rights information was available.</returns>
    public bool HasRightsInformation()
    {
        return rights != null;
    }

    /// <summary>Gets the interpreted source rights.</summary>
    /// <returns>The rights report, or null when OS/2 rights were unavailable.</returns>
    public FontEmbeddingRights? GetRights()
    {
        return rights;
    }

    /// <summary>Gets the embedding mode requested by the caller.</summary>
    /// <returns>The requested mode.</returns>
    public FontSubsetEmbeddingMode GetRequestedEmbeddingMode()
    {
        return requestedMode;
    }

    /// <summary>Gets the embedding mode selected after applying rights policy.</summary>
    /// <returns>The effective mode.</returns>
    public FontSubsetEmbeddingMode GetEffectiveEmbeddingMode()
    {
        return effectiveMode;
    }

    /// <summary>Gets the policy used to resolve rights conflicts.</summary>
    /// <returns>The selected rights policy.</returns>
    public FontSubsetRightsPolicy GetPolicy()
    {
        return policy;
    }

    /// <summary>Reports whether the requested output conflicts with source rights.</summary>
    /// <returns>True when the request conflicts with source rights.</returns>
    public bool HasPolicyConflict()
    {
        return policyConflict;
    }

    /// <summary>Reports whether a conflict was returned without changing output.</summary>
    /// <returns>True for a report-only conflict.</returns>
    public bool WasConflictReported()
    {
        return reportedConflict;
    }

    /// <summary>Reports whether no-subsetting rights forced complete-font output.</summary>
    /// <returns>True when subset output was changed to full-font output.</returns>
    public bool WasFullFontForced()
    {
        return forcedFullFont;
    }

    /// <summary>Gets the number of active rights diagnostics.</summary>
    /// <returns>The diagnostic count.</returns>
    public int GetDiagnosticCount()
    {
        return diagnostics.Length;
    }

    /// <summary>Gets one active rights diagnostic.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the diagnostic list.
    /// </exception>
    public FontDiagnostic GetDiagnostic(int index)
    {
        if (index < 0 || index >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return diagnostics[index];
    }

    internal FontDiagnostic[] CopyDiagnostics()
    {
        return CopyDiagnostics(diagnostics);
    }

    static FontDiagnostic[] CopyDiagnostics(FontDiagnostic[] values)
    {
        FontDiagnostic[] copy = new FontDiagnostic[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
