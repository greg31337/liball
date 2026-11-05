namespace ttf;

/// <summary>Defines one immutable fallback decision reported during shaping.</summary>
public interface IFontFallbackDecision
{
    /// <summary>Gets the machine-readable decision kind.</summary>
    /// <returns>The fallback decision kind.</returns>
    FontFallbackDecisionKind GetKind();

    /// <summary>Gets the affected source UTF-16 start.</summary>
    /// <returns>The zero-based source offset.</returns>
    int GetUtf16Start();

    /// <summary>Gets the affected source UTF-16 length.</summary>
    /// <returns>The nonnegative source length.</returns>
    int GetUtf16Length();

    /// <summary>Gets the canonical script associated with the decision.</summary>
    /// <returns>The four-letter script code, or an empty string.</returns>
    string GetScript();

    /// <summary>Gets the caller-requested family name.</summary>
    /// <returns>The concrete or generic requested family name.</returns>
    string GetRequestedFamilyName();

    /// <summary>Gets the selected configured family name.</summary>
    /// <returns>The selected family name, or an empty string.</returns>
    string GetResolvedFamilyName();

    /// <summary>Gets the selected face when one applies.</summary>
    /// <returns>The selected face, or null.</returns>
    FontFace? GetFace();

    /// <summary>Gets the selected instance when one applies.</summary>
    /// <returns>The selected instance, or null.</returns>
    FontInstance? GetInstance();

    /// <summary>Gets the first missing Unicode scalar.</summary>
    /// <returns>The Unicode scalar, or <c>-1</c>.</returns>
    int GetCodePoint();

    /// <summary>Gets the unsupported shaping table tag.</summary>
    /// <returns>The big-endian table tag, or zero.</returns>
    int GetTableTag();
}
