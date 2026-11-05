namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontSubsetOptions"/>.</summary>
public interface IFontSubsetOptions
{
    /// <summary>Gets the requested embedding mode.</summary>
    /// <returns>The subset or full-font mode.</returns>
    FontSubsetEmbeddingMode GetEmbeddingMode();

    /// <summary>Sets the requested embedding mode.</summary>
    /// <param name="value">The subset or full-font mode.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    /// <exception cref="System.ArgumentException">
    /// Subset mode conflicts with compact glyph identifiers and retained unknown tables.
    /// </exception>
    void SetEmbeddingMode(FontSubsetEmbeddingMode value);

    /// <summary>Gets the glyph identifier retention mode.</summary>
    /// <returns>The compact or preserve mode.</returns>
    FontSubsetGlyphRetentionMode GetGlyphRetentionMode();

    /// <summary>Sets the glyph identifier retention mode.</summary>
    /// <param name="value">The compact or preserve mode.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    /// <exception cref="System.ArgumentException">
    /// Compact subset output conflicts with retained unknown tables.
    /// </exception>
    void SetGlyphRetentionMode(FontSubsetGlyphRetentionMode value);

    /// <summary>Gets the recommended glyph retention policy.</summary>
    /// <returns>The recommended glyph policy.</returns>
    FontSubsetRecommendedGlyphPolicy GetRecommendedGlyphPolicy();

    /// <summary>Sets the recommended glyph retention policy.</summary>
    /// <param name="value">The recommended glyph policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is undefined.
    /// </exception>
    void SetRecommendedGlyphPolicy(FontSubsetRecommendedGlyphPolicy value);

    /// <summary>Gets the hint retention policy.</summary>
    /// <returns>The hint policy.</returns>
    FontSubsetHintPolicy GetHintPolicy();

    /// <summary>Sets the hint retention policy.</summary>
    /// <param name="value">The hint policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    void SetHintPolicy(FontSubsetHintPolicy value);

    /// <summary>Gets the layout retention policy.</summary>
    /// <returns>The layout policy.</returns>
    FontSubsetLayoutPolicy GetLayoutPolicy();

    /// <summary>Sets the layout retention policy.</summary>
    /// <param name="value">The layout policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    void SetLayoutPolicy(FontSubsetLayoutPolicy value);

    /// <summary>Gets the vertical table retention policy.</summary>
    /// <returns>The vertical policy.</returns>
    FontSubsetVerticalPolicy GetVerticalPolicy();

    /// <summary>Sets the vertical table retention policy.</summary>
    /// <param name="value">The vertical policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    void SetVerticalPolicy(FontSubsetVerticalPolicy value);

    /// <summary>Gets the color table retention policy.</summary>
    /// <returns>The color policy.</returns>
    FontSubsetColorPolicy GetColorPolicy();

    /// <summary>Sets the color table retention policy.</summary>
    /// <param name="value">The color policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    void SetColorPolicy(FontSubsetColorPolicy value);

    /// <summary>Gets the metadata retention policy.</summary>
    /// <returns>The metadata policy.</returns>
    FontSubsetMetadataPolicy GetMetadataPolicy();

    /// <summary>Sets the metadata retention policy.</summary>
    /// <param name="value">The metadata policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    void SetMetadataPolicy(FontSubsetMetadataPolicy value);

    /// <summary>Gets the unknown table retention policy.</summary>
    /// <returns>The unknown table policy.</returns>
    FontSubsetUnknownTablePolicy GetUnknownTablePolicy();

    /// <summary>Sets the unknown table retention policy.</summary>
    /// <param name="value">The unknown table policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    /// <exception cref="System.ArgumentException">
    /// Safe retention conflicts with compact subset glyph identifiers.
    /// </exception>
    void SetUnknownTablePolicy(FontSubsetUnknownTablePolicy value);

    /// <summary>Gets the embedding-right response policy.</summary>
    /// <returns>The rights policy.</returns>
    FontSubsetRightsPolicy GetRightsPolicy();

    /// <summary>Sets the embedding-right response policy.</summary>
    /// <param name="value">The rights policy.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is undefined.</exception>
    void SetRightsPolicy(FontSubsetRightsPolicy value);

    /// <summary>Gets the caller-selected subset prefix override.</summary>
    /// <returns>
    /// The six-uppercase-letter prefix, or null when a deterministic prefix
    /// should be generated.
    /// </returns>
    string? GetSubsetPrefixOverride();

    /// <summary>Sets or clears the subset prefix collision override.</summary>
    /// <param name="value">
    /// Exactly six ASCII uppercase letters, or null to use deterministic
    /// generation.
    /// </param>
    /// <exception cref="System.ArgumentException">
    /// <c>value</c> is non-null and is not exactly six ASCII uppercase letters.
    /// </exception>
    void SetSubsetPrefixOverride(string? value);

    /// <summary>Gets shared policy, limits, and cancellation controls.</summary>
    /// <returns>An independent mutable copy.</returns>
    FontOperationOptions GetOperationOptions();

    /// <summary>Sets shared operation controls from an owned snapshot.</summary>
    /// <param name="value">The operation controls.</param>
    void SetOperationOptions(FontOperationOptions value);
}
