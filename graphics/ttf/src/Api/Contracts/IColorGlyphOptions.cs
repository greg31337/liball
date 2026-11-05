namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ColorGlyphOptions"/>.</summary>
public interface IColorGlyphOptions
{
    /// <summary>Gets the selected zero-based CPAL palette index.</summary>
    /// <returns>The palette index.</returns>
    int GetPaletteIndex();

    /// <summary>Sets the zero-based CPAL palette index.</summary>
    /// <param name="value">The nonnegative palette index.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is negative.</exception>
    void SetPaletteIndex(int value);

    /// <summary>Gets the preferred representation.</summary>
    /// <returns>The preferred representation, or none for automatic selection.</returns>
    ColorGlyphRepresentation GetPreferredRepresentation();

    /// <summary>Sets the preferred representation.</summary>
    /// <remarks>
    /// None requests automatic selection. An unavailable preference falls back
    /// through the deterministic automatic order.
    /// </remarks>
    /// <param name="value">The preferred representation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is not a defined representation.
    /// </exception>
    void SetPreferredRepresentation(ColorGlyphRepresentation value);

    /// <summary>Gets the color used by COLR foreground-color references.</summary>
    /// <returns>The foreground color.</returns>
    ColorGlyphColor GetForegroundColor();

    /// <summary>Sets the color used by COLR foreground-color references.</summary>
    /// <param name="value">The foreground color.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    void SetForegroundColor(ColorGlyphColor value);

    /// <summary>Reports whether a target bitmap strike size was supplied.</summary>
    /// <returns>True when target dimensions are present; otherwise false.</returns>
    bool HasBitmapTargetSize();

    /// <summary>Gets the target horizontal pixels-per-em size.</summary>
    /// <returns>The target size, or zero when none is supplied.</returns>
    int GetTargetPixelsPerEmX();

    /// <summary>Gets the target vertical pixels-per-em size.</summary>
    /// <returns>The target size, or zero when none is supplied.</returns>
    int GetTargetPixelsPerEmY();

    /// <summary>Sets the target dimensions used to choose a bitmap strike.</summary>
    /// <param name="pixelsPerEmX">The positive horizontal target size.</param>
    /// <param name="pixelsPerEmY">The positive vertical target size.</param>
    void SetBitmapTargetSize(int pixelsPerEmX, int pixelsPerEmY);

    /// <summary>Clears the target bitmap strike size.</summary>
    void ClearBitmapTargetSize();

    /// <summary>Gets shared policy, limits, and cancellation controls.</summary>
    /// <returns>An independent mutable copy.</returns>
    FontOperationOptions GetOperationOptions();

    /// <summary>Sets shared operation controls from an owned snapshot.</summary>
    /// <param name="value">The operation controls.</param>
    void SetOperationOptions(FontOperationOptions value);
}
