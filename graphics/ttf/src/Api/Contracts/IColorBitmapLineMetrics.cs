namespace ttf;

/// <summary>Defines one embedded-bitmap strike's line metrics.</summary>
public interface IColorBitmapLineMetrics
{
    /// <summary>Gets the ascender in pixels.</summary>
    /// <returns>The ascender.</returns>
    int GetAscender();
    /// <summary>Gets the descender in pixels.</summary>
    /// <returns>The descender.</returns>
    int GetDescender();
    /// <summary>Gets the maximum glyph width in pixels.</summary>
    /// <returns>The maximum width.</returns>
    int GetMaximumWidth();
    /// <summary>Gets the caret-slope numerator.</summary>
    /// <returns>The numerator.</returns>
    int GetCaretSlopeNumerator();
    /// <summary>Gets the caret-slope denominator.</summary>
    /// <returns>The denominator.</returns>
    int GetCaretSlopeDenominator();
    /// <summary>Gets the caret offset.</summary>
    /// <returns>The offset.</returns>
    int GetCaretOffset();
    /// <summary>Gets the minimum origin-side bearing.</summary>
    /// <returns>The bearing.</returns>
    int GetMinimumOriginSideBearing();
    /// <summary>Gets the minimum advance-side bearing.</summary>
    /// <returns>The bearing.</returns>
    int GetMinimumAdvanceSideBearing();
    /// <summary>Gets the maximum extent before the baseline.</summary>
    /// <returns>The extent.</returns>
    int GetMaximumBeforeBaseline();
    /// <summary>Gets the minimum extent after the baseline.</summary>
    /// <returns>The extent.</returns>
    int GetMinimumAfterBaseline();
}
