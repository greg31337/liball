namespace ttf;

/// <summary>Defines the deterministic fallback report for one shaping operation.</summary>
public interface IFontFallbackReport
{
    /// <summary>Gets the fallback decision count.</summary>
    /// <returns>The decision count.</returns>
    int GetDecisionCount();

    /// <summary>Gets one fallback decision in stable source order.</summary>
    /// <param name="index">The zero-based decision index.</param>
    /// <returns>The immutable decision.</returns>
    FontFallbackDecision GetDecision(int index);

    /// <summary>Reports whether a partial-coverage face handled a missing glyph.</summary>
    /// <returns>True when a final missing-glyph face exists.</returns>
    bool HasFinalMissingGlyphFace();

    /// <summary>Gets the final partial-coverage face used for a missing glyph.</summary>
    /// <returns>The final missing-glyph face, or null.</returns>
    FontFace? GetFinalMissingGlyphFace();

    /// <summary>Gets the final partial-coverage instance used for a missing glyph.</summary>
    /// <returns>The final missing-glyph instance, or null.</returns>
    FontInstance? GetFinalMissingGlyphInstance();
}
