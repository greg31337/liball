namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontSubsetBuilder"/>.</summary>
public interface IFontSubsetBuilder
{
    /// <summary>Gets the immutable font instance bound to this builder.</summary>
    /// <returns>The constructor-supplied source instance.</returns>
    FontInstance GetSourceInstance();

    /// <summary>Gets a copy of the effective subset options.</summary>
    /// <returns>A defensive copy of the constructor-time option snapshot.</returns>
    FontSubsetOptions GetEffectiveOptions();

    /// <summary>Adds one Unicode scalar request.</summary>
    /// <param name="codePoint">The Unicode scalar value.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>codePoint</c> is not a Unicode scalar value.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    void AddCodePoint(int codePoint);

    /// <summary>Adds every Unicode scalar in a UTF-16 string.</summary>
    /// <param name="text">Strictly valid UTF-16 text.</param>
    /// <exception cref="System.ArgumentNullException"><c>text</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// <c>text</c> contains an unpaired surrogate.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request or decoded-rune limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    void AddText(string text);

    /// <summary>Adds one exact sequence of Unicode scalar values.</summary>
    /// <remarks>
    /// Every scalar is also retained as a nominal character request. The exact
    /// sequence is used for variation-sequence discovery and deterministic
    /// subset identity.
    /// </remarks>
    /// <param name="unicodeSequence">A nonempty sequence of Unicode scalars.</param>
    void AddUnicodeSequence(int[] unicodeSequence);

    /// <summary>Adds one raw glyph identifier.</summary>
    /// <param name="glyphId">The glyph identifier in this builder's instance.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>glyphId</c> is outside the font instance.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    void AddGlyph(int glyphId);

    /// <summary>Adds a glyph with its exact Unicode scalar sequence.</summary>
    /// <remarks>
    /// The association is retained even when the source cmap maps the sequence
    /// differently. Its scalars and exact sequence are also added as character
    /// requests.
    /// </remarks>
    /// <param name="glyphId">The glyph identifier in this builder's instance.</param>
    /// <param name="unicodeSequence">A nonempty sequence of Unicode scalars.</param>
    void AddGlyphUnicodeSequence(int glyphId, int[] unicodeSequence);

    /// <summary>Adds glyph and exact Unicode requests from a shaped run.</summary>
    /// <param name="run">
    /// A run shaped for this builder's font instance.
    /// </param>
    /// <exception cref="System.ArgumentNullException"><c>run</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// The run contains a glyph outside this builder's font instance or an
    /// invalid Unicode scalar.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    void AddShapedRun(ShapedRun run);

    /// <summary>Adds one fallback-resolved shaping run from this builder's instance.</summary>
    /// <param name="run">The resolved shaping run.</param>
    /// <exception cref="System.ArgumentNullException"><c>run</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// The run was resolved to a different font instance.
    /// </exception>
    void AddResolvedRun(TextShapingRun run);

    /// <summary>Adds one OpenType feature tag request.</summary>
    /// <param name="featureTag">A four-byte printable ASCII OpenType tag.</param>
    /// <exception cref="System.ArgumentException">
    /// <c>featureTag</c> is not a printable four-byte tag.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    void AddFeature(int featureTag);

    /// <summary>Adds one CPAL palette index request.</summary>
    /// <param name="paletteIndex">The zero-based palette index.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The font has no such palette.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The configured request limit is exceeded.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The builder has already been built.
    /// </exception>
    void AddPalette(int paletteIndex);

    /// <summary>Freezes and returns the deterministic subset request.</summary>
    /// <remarks>
    /// The first call sorts every set-like input and permanently prevents
    /// additions. Later calls return the same immutable result.
    /// </remarks>
    /// <returns>The immutable subset request.</returns>
    FontSubset Build();

    /// <summary>Builds the request and generates its complete output result.</summary>
    /// <returns>The generated standalone font and all associated reports.</returns>
    FontSubsetResult BuildResult();
}
