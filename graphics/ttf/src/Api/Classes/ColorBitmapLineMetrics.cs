namespace ttf;

/// <summary>Represents one embedded-bitmap strike's line metrics.</summary>
public class ColorBitmapLineMetrics : IColorBitmapLineMetrics
{
    int ascender;
    int descender;
    int maximumWidth;
    int caretSlopeNumerator;
    int caretSlopeDenominator;
    int caretOffset;
    int minimumOriginSideBearing;
    int minimumAdvanceSideBearing;
    int maximumBeforeBaseline;
    int minimumAfterBaseline;

    internal ColorBitmapLineMetrics(
        int ascender,
        int descender,
        int maximumWidth,
        int caretSlopeNumerator,
        int caretSlopeDenominator,
        int caretOffset,
        int minimumOriginSideBearing,
        int minimumAdvanceSideBearing,
        int maximumBeforeBaseline,
        int minimumAfterBaseline)
    {
        this.ascender = ascender;
        this.descender = descender;
        this.maximumWidth = maximumWidth;
        this.caretSlopeNumerator = caretSlopeNumerator;
        this.caretSlopeDenominator = caretSlopeDenominator;
        this.caretOffset = caretOffset;
        this.minimumOriginSideBearing = minimumOriginSideBearing;
        this.minimumAdvanceSideBearing = minimumAdvanceSideBearing;
        this.maximumBeforeBaseline = maximumBeforeBaseline;
        this.minimumAfterBaseline = minimumAfterBaseline;
    }

    /// <summary>Gets the ascender in pixels.</summary>
    /// <returns>The ascender.</returns>
    public int GetAscender() { return ascender; }
    /// <summary>Gets the descender in pixels.</summary>
    /// <returns>The descender.</returns>
    public int GetDescender() { return descender; }
    /// <summary>Gets the maximum glyph width in pixels.</summary>
    /// <returns>The maximum width.</returns>
    public int GetMaximumWidth() { return maximumWidth; }
    /// <summary>Gets the caret-slope numerator.</summary>
    /// <returns>The numerator.</returns>
    public int GetCaretSlopeNumerator() { return caretSlopeNumerator; }
    /// <summary>Gets the caret-slope denominator.</summary>
    /// <returns>The denominator.</returns>
    public int GetCaretSlopeDenominator() { return caretSlopeDenominator; }
    /// <summary>Gets the caret offset.</summary>
    /// <returns>The offset.</returns>
    public int GetCaretOffset() { return caretOffset; }
    /// <summary>Gets the minimum origin-side bearing.</summary>
    /// <returns>The bearing.</returns>
    public int GetMinimumOriginSideBearing() { return minimumOriginSideBearing; }
    /// <summary>Gets the minimum advance-side bearing.</summary>
    /// <returns>The bearing.</returns>
    public int GetMinimumAdvanceSideBearing() { return minimumAdvanceSideBearing; }
    /// <summary>Gets the maximum extent before the baseline.</summary>
    /// <returns>The extent.</returns>
    public int GetMaximumBeforeBaseline() { return maximumBeforeBaseline; }
    /// <summary>Gets the minimum extent after the baseline.</summary>
    /// <returns>The extent.</returns>
    public int GetMinimumAfterBaseline() { return minimumAfterBaseline; }
}
