namespace ttf;

/// <summary>Reports every deterministic font-fallback decision from one shaping operation.</summary>
/// <remarks>
/// Decisions are ordered by source UTF-16 range and then by decision kind. Callers
/// can use the typed findings to reject output, issue warnings, or apply their own
/// replacement policy without parsing diagnostic messages.
/// </remarks>
public class FontFallbackReport : IFontFallbackReport
{
    FontFallbackDecision[] decisions;
    FontFace? finalMissingGlyphFace;
    FontInstance? finalMissingGlyphInstance;

    internal FontFallbackReport(FontFallbackDecision[] decisions)
    {
        this.decisions = CopyDecisions(decisions);
        int index = 0;
        while (index < this.decisions.Length)
        {
            FontFallbackDecision decision = this.decisions[index];
            if (decision.GetKind() ==
                    FontFallbackDecisionKind.FinalMissingGlyphFace &&
                decision.GetFace() != null)
            {
                finalMissingGlyphFace = decision.GetFace();
                finalMissingGlyphInstance = decision.GetInstance();
            }
            index += 1;
        }
    }

    /// <summary>Gets the fallback decision count.</summary>
    /// <returns>The decision count.</returns>
    public int GetDecisionCount() { return decisions.Length; }

    /// <summary>Gets one fallback decision in stable source order.</summary>
    /// <param name="index">The zero-based decision index.</param>
    /// <returns>The immutable decision.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public FontFallbackDecision GetDecision(int index)
    {
        if (index < 0 || index >= decisions.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return decisions[index];
    }

    /// <summary>Reports whether a partial-coverage face handled a missing glyph.</summary>
    /// <returns>True when a final missing-glyph face exists.</returns>
    public bool HasFinalMissingGlyphFace()
    {
        return finalMissingGlyphFace != null;
    }

    /// <summary>Gets the final partial-coverage face used for a missing glyph.</summary>
    /// <returns>The final missing-glyph face, or null.</returns>
    public FontFace? GetFinalMissingGlyphFace()
    {
        return finalMissingGlyphFace;
    }

    /// <summary>Gets the final partial-coverage instance used for a missing glyph.</summary>
    /// <returns>The final missing-glyph instance, or null.</returns>
    public FontInstance? GetFinalMissingGlyphInstance()
    {
        return finalMissingGlyphInstance;
    }

    static FontFallbackDecision[] CopyDecisions(
        FontFallbackDecision[] values)
    {
        FontFallbackDecision[] copy =
            new FontFallbackDecision[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
