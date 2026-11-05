namespace ttf;

/// <summary>Controls palette and representation selection for one color glyph.</summary>
public class ColorGlyphOptions : IColorGlyphOptions
{
    int paletteIndex;
    ColorGlyphRepresentation preferredRepresentation;
    ColorGlyphColor foregroundColor;
    int targetPixelsPerEmX;
    int targetPixelsPerEmY;
    FontOperationOptions operationOptions;

    /// <summary>Creates options that use palette zero and automatic representation selection.</summary>
    public ColorGlyphOptions()
    {
        paletteIndex = 0;
        preferredRepresentation = ColorGlyphRepresentation.None;
        foregroundColor = new ColorGlyphColor(0, 0, 0, 255);
        operationOptions = new FontOperationOptions();
    }

    /// <summary>Gets the selected zero-based CPAL palette index.</summary>
    /// <returns>The palette index.</returns>
    public int GetPaletteIndex() { return paletteIndex; }

    /// <summary>Sets the zero-based CPAL palette index.</summary>
    /// <param name="value">The nonnegative palette index.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>value</c> is negative.</exception>
    public void SetPaletteIndex(int value)
    {
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        paletteIndex = value;
    }

    /// <summary>Gets the preferred representation.</summary>
    /// <returns>The preferred representation, or none for automatic selection.</returns>
    public ColorGlyphRepresentation GetPreferredRepresentation()
    {
        return preferredRepresentation;
    }

    /// <summary>Sets the preferred representation.</summary>
    /// <remarks>
    /// None requests automatic selection. An unavailable preference falls back
    /// through the deterministic automatic order.
    /// </remarks>
    /// <param name="value">The preferred representation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>value</c> is not a defined representation.
    /// </exception>
    public void SetPreferredRepresentation(ColorGlyphRepresentation value)
    {
        if (value < ColorGlyphRepresentation.None ||
            value > ColorGlyphRepresentation.MonochromeOutline)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        preferredRepresentation = value;
    }

    /// <summary>Gets the color used by COLR foreground-color references.</summary>
    /// <returns>The foreground color.</returns>
    public ColorGlyphColor GetForegroundColor() { return foregroundColor; }

    /// <summary>Sets the color used by COLR foreground-color references.</summary>
    /// <param name="value">The foreground color.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    public void SetForegroundColor(ColorGlyphColor value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        foregroundColor = value;
    }

    /// <summary>Reports whether a target bitmap strike size was supplied.</summary>
    /// <returns>True when target dimensions are present; otherwise false.</returns>
    public bool HasBitmapTargetSize() { return targetPixelsPerEmX > 0; }

    /// <summary>Gets the target horizontal pixels-per-em size.</summary>
    /// <returns>The target size, or zero when none is supplied.</returns>
    public int GetTargetPixelsPerEmX() { return targetPixelsPerEmX; }

    /// <summary>Gets the target vertical pixels-per-em size.</summary>
    /// <returns>The target size, or zero when none is supplied.</returns>
    public int GetTargetPixelsPerEmY() { return targetPixelsPerEmY; }

    /// <summary>Sets the target dimensions used to choose a bitmap strike.</summary>
    /// <param name="pixelsPerEmX">The positive horizontal target size.</param>
    /// <param name="pixelsPerEmY">The positive vertical target size.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// A target is outside the range from one through 65535.
    /// </exception>
    public void SetBitmapTargetSize(int pixelsPerEmX, int pixelsPerEmY)
    {
        ValidateTargetSize(pixelsPerEmX, "pixelsPerEmX");
        ValidateTargetSize(pixelsPerEmY, "pixelsPerEmY");
        targetPixelsPerEmX = pixelsPerEmX;
        targetPixelsPerEmY = pixelsPerEmY;
    }

    /// <summary>Clears the target bitmap strike size.</summary>
    public void ClearBitmapTargetSize()
    {
        targetPixelsPerEmX = 0;
        targetPixelsPerEmY = 0;
    }

    /// <summary>Gets shared policy, limits, and cancellation controls.</summary>
    /// <returns>An independent mutable copy.</returns>
    public FontOperationOptions GetOperationOptions()
    {
        return operationOptions.Copy();
    }

    /// <summary>Sets shared operation controls from an owned snapshot.</summary>
    /// <param name="value">The operation controls.</param>
    /// <exception cref="System.ArgumentNullException"><c>value</c> is null.</exception>
    public void SetOperationOptions(FontOperationOptions value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        operationOptions = value.Copy();
    }

    internal FontOperationOptions GetOperationSettings()
    {
        return operationOptions;
    }

    internal ColorGlyphOptions Copy()
    {
        ColorGlyphOptions copy = new ColorGlyphOptions();
        copy.paletteIndex = paletteIndex;
        copy.preferredRepresentation = preferredRepresentation;
        copy.foregroundColor = foregroundColor;
        copy.targetPixelsPerEmX = targetPixelsPerEmX;
        copy.targetPixelsPerEmY = targetPixelsPerEmY;
        copy.operationOptions = operationOptions.Copy();
        return copy;
    }

    static void ValidateTargetSize(int value, string parameterName)
    {
        if (value < 1 || value > 65535)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
