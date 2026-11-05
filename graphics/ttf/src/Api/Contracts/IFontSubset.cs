namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontSubset"/>.</summary>
public interface IFontSubset
{
    /// <summary>
    /// Copies the map from original glyph identifiers to output identifiers.
    /// </summary>
    /// <remarks>
    /// An entry is <c>-1</c> when the original glyph is omitted.
    /// </remarks>
    /// <returns>A new array indexed by original glyph identifier.</returns>
    int[] GetOldToNewGlyphMap();

    /// <summary>
    /// Copies the map from output glyph identifiers to original identifiers.
    /// </summary>
    /// <remarks>
    /// An entry is <c>-1</c> for an unassigned preserve-GID gap.
    /// </remarks>
    /// <returns>A new array indexed by output glyph identifier.</returns>
    int[] GetNewToOldGlyphMap();

    /// <summary>Gets the explicit embedding-rights policy decision.</summary>
    /// <returns>The immutable embedding decision report.</returns>
    FontSubsetEmbeddingDecision GetEmbeddingDecision();

    /// <summary>Gets every computed dependency closure for this subset.</summary>
    /// <returns>An immutable closure report using source-font indices.</returns>
    FontSubsetClosureReport GetClosureReport();

    /// <summary>Copies this request as a generated standalone SFNT font.</summary>
    /// <returns>A new array containing the subset or complete static font.</returns>
    byte[] CopyFontBytes();

    /// <summary>Gets the deterministic PostScript-compatible output name.</summary>
    /// <returns>The subset-prefixed name, or the full-font name.</returns>
    string GetSubsetName();

    /// <summary>Generates the standalone font and its complete result report.</summary>
    /// <returns>
    /// An immutable result containing bytes, names, maps, table disposition,
    /// repairs, embedding decisions, and diagnostics.
    /// </returns>
    /// <exception cref="FontFormatException">
    /// A required source table or retained structure cannot be rewritten.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// Generated output exceeds a configured resource limit.
    /// </exception>
    FontSubsetResult CreateResult();

    /// <summary>Copies diagnostics produced while constructing the subset.</summary>
    /// <returns>A new array in deterministic processing order.</returns>
    FontDiagnostic[] GetDiagnostics();
}
