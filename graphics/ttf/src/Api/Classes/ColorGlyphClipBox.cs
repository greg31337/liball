namespace ttf;

/// <summary>Represents one resolved COLR glyph clip box.</summary>
public class ColorGlyphClipBox : IColorGlyphClipBox
{
    int sourceFormat;
    bool supported;
    double xMinimum;
    double yMinimum;
    double xMaximum;
    double yMaximum;

    internal ColorGlyphClipBox(
        int sourceFormat,
        bool supported,
        double xMinimum,
        double yMinimum,
        double xMaximum,
        double yMaximum)
    {
        this.sourceFormat = sourceFormat;
        this.supported = supported;
        this.xMinimum = xMinimum;
        this.yMinimum = yMinimum;
        this.xMaximum = xMaximum;
        this.yMaximum = yMaximum;
    }

    /// <summary>Gets the source clip-box format.</summary>
    /// <returns>One for static coordinates or two for variable coordinates.</returns>
    public int GetSourceFormat() { return sourceFormat; }
    /// <summary>Reports whether the clip-box format is supported.</summary>
    /// <returns>True when resolved coordinates are available; otherwise false.</returns>
    public bool IsSupported() { return supported; }
    /// <summary>Reports whether the source clip box is variable.</summary>
    /// <returns>True for a variable source clip box; otherwise false.</returns>
    public bool IsVariable() { return sourceFormat == 2; }
    /// <summary>Gets the minimum x coordinate.</summary>
    /// <returns>The resolved design-unit coordinate.</returns>
    public double GetXMinimum()
    {
        ValidateSupported();
        return xMinimum;
    }
    /// <summary>Gets the minimum y coordinate.</summary>
    /// <returns>The resolved design-unit coordinate.</returns>
    public double GetYMinimum()
    {
        ValidateSupported();
        return yMinimum;
    }
    /// <summary>Gets the maximum x coordinate.</summary>
    /// <returns>The resolved design-unit coordinate.</returns>
    public double GetXMaximum()
    {
        ValidateSupported();
        return xMaximum;
    }
    /// <summary>Gets the maximum y coordinate.</summary>
    /// <returns>The resolved design-unit coordinate.</returns>
    public double GetYMaximum()
    {
        ValidateSupported();
        return yMaximum;
    }

    void ValidateSupported()
    {
        if (!supported)
        {
            throw new System.InvalidOperationException(
                "This COLR clip-box format is unsupported.");
        }
    }
}
