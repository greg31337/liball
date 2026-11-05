namespace ttf;

/// <summary>Represents one node in a bounded, normalized COLR paint graph.</summary>
public class ColorPaint : IColorPaint
{
    ColorPaintKind kind;
    int sourceFormat;
    ColorPaint[] children;
    int glyphId;
    int paletteEntryIndex;
    double alpha;
    double[] values;
    int extendMode;
    ColorPaintStop[]? stops;
    int compositeMode;
    ColorGlyphColor? color;
    ColorAffineTransform? transform;
    ColorLinearGradient? linearGradient;
    ColorRadialGradient? radialGradient;
    ColorSweepGradient? sweepGradient;

    internal ColorPaint(
        ColorPaintKind kind,
        int sourceFormat,
        ColorPaint[] children,
        int glyphId,
        int paletteEntryIndex,
        double alpha,
        double[] values,
        int extendMode,
        ColorPaintStop[]? stops,
        int compositeMode,
        ColorGlyphColor? color)
    {
        this.kind = kind;
        this.sourceFormat = sourceFormat;
        this.children = children;
        this.glyphId = glyphId;
        this.paletteEntryIndex = paletteEntryIndex;
        this.alpha = alpha;
        this.values = values;
        this.extendMode = extendMode;
        this.stops = stops;
        this.compositeMode = compositeMode;
        this.color = color;

        if (kind == ColorPaintKind.Transform)
        {
            transform = new ColorAffineTransform(values);
        }
        if (kind == ColorPaintKind.LinearGradient)
        {
            linearGradient = new ColorLinearGradient(values);
        }
        if (kind == ColorPaintKind.RadialGradient)
        {
            radialGradient = new ColorRadialGradient(values);
        }
        if (kind == ColorPaintKind.SweepGradient)
        {
            sweepGradient = new ColorSweepGradient(values);
        }
    }

    /// <summary>Gets this node's normalized operation.</summary>
    /// <returns>The paint kind.</returns>
    public ColorPaintKind GetKind() { return kind; }

    /// <summary>Gets the source COLR paint format.</summary>
    /// <returns>The encoded paint format, or zero for a COLR version zero node.</returns>
    public int GetSourceFormat() { return sourceFormat; }

    /// <summary>Gets the strongly typed source COLR paint format.</summary>
    /// <returns>The format, or unsupported when the encoded format is unknown.</returns>
    public ColorPaintFormat GetFormat()
    {
        if (sourceFormat < 0 || sourceFormat > 32)
        {
            return ColorPaintFormat.Unsupported;
        }
        return (ColorPaintFormat)sourceFormat;
    }

    /// <summary>Gets the number of child paints.</summary>
    /// <returns>The child count.</returns>
    public int GetChildCount() { return children.Length; }

    /// <summary>Gets one child paint.</summary>
    /// <param name="index">The zero-based child index.</param>
    /// <returns>The child paint.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the node.</exception>
    public ColorPaint GetChild(int index)
    {
        if (index < 0 || index >= children.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return children[index];
    }

    /// <summary>Gets the number of paints in a layer-list node.</summary>
    /// <returns>The layer count.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a layer list.</exception>
    public int GetLayerCount()
    {
        if (kind != ColorPaintKind.LayerList)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a layer list.");
        }
        return children.Length;
    }

    /// <summary>Gets one paint from a layer-list node in paint order.</summary>
    /// <param name="index">The zero-based layer index.</param>
    /// <returns>The layer paint.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a layer list.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public ColorPaint GetLayer(int index)
    {
        if (kind != ColorPaintKind.LayerList)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a layer list.");
        }
        return GetChild(index);
    }

    /// <summary>Gets the glyph identifier referenced by a glyph paint.</summary>
    /// <returns>The referenced glyph identifier.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a glyph paint.</exception>
    public int GetGlyphId()
    {
        if (kind != ColorPaintKind.Glyph && kind != ColorPaintKind.ColrGlyph)
        {
            throw new System.InvalidOperationException(
                "This color paint does not reference a glyph.");
        }

        return glyphId;
    }

    /// <summary>Gets the fill paint applied by a glyph paint.</summary>
    /// <returns>The glyph fill paint.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a glyph paint.</exception>
    public ColorPaint GetGlyphPaint()
    {
        if (kind != ColorPaintKind.Glyph)
        {
            throw new System.InvalidOperationException(
                "This color paint does not fill a glyph.");
        }
        return children[0];
    }

    /// <summary>Gets the expanded paint referenced by a COLR-glyph paint.</summary>
    /// <returns>The referenced glyph's normalized paint.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a COLR-glyph paint.</exception>
    public ColorPaint GetReferencedPaint()
    {
        if (kind != ColorPaintKind.ColrGlyph)
        {
            throw new System.InvalidOperationException(
                "This color paint does not reference a COLR glyph.");
        }
        return children[0];
    }

    /// <summary>Gets the CPAL entry used by a palette-color paint.</summary>
    /// <returns>The zero-based palette entry index.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a palette-color paint.</exception>
    public int GetPaletteEntryIndex()
    {
        if (kind != ColorPaintKind.PaletteColor)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a palette color.");
        }

        return paletteEntryIndex;
    }

    /// <summary>Gets the resolved RGBA color of a color-reference paint.</summary>
    /// <returns>The selected palette or foreground color.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a color-reference paint.</exception>
    public ColorGlyphColor GetColor()
    {
        if (color == null)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a color reference.");
        }

        return color;
    }

    /// <summary>Gets the paint alpha multiplier of a color-reference paint.</summary>
    /// <returns>The alpha multiplier.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a color-reference paint.</exception>
    public double GetAlpha()
    {
        if (color == null)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a color reference.");
        }

        return alpha;
    }

    /// <summary>Gets the number of normalized numeric values on this paint.</summary>
    /// <returns>The value count.</returns>
    public int GetValueCount() { return values.Length; }

    /// <summary>Gets one normalized numeric paint value.</summary>
    /// <param name="index">The zero-based value index.</param>
    /// <returns>The numeric value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the node.</exception>
    public double GetValue(int index)
    {
        if (index < 0 || index >= values.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return values[index];
    }

    /// <summary>Gets the resolved affine matrix of a transform paint.</summary>
    /// <returns>The normalized affine transform.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a transform paint.</exception>
    public ColorAffineTransform GetTransform()
    {
        if (transform == null)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a transform.");
        }
        return transform;
    }

    /// <summary>Gets the child affected by a transform paint.</summary>
    /// <returns>The transformed child paint.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a transform paint.</exception>
    public ColorPaint GetTransformedPaint()
    {
        if (kind != ColorPaintKind.Transform)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a transform.");
        }
        return children[0];
    }

    /// <summary>Gets resolved linear-gradient geometry.</summary>
    /// <returns>The linear-gradient geometry.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a linear gradient.</exception>
    public ColorLinearGradient GetLinearGradient()
    {
        if (linearGradient == null)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a linear gradient.");
        }
        return linearGradient;
    }

    /// <summary>Gets resolved radial-gradient geometry.</summary>
    /// <returns>The radial-gradient geometry.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a radial gradient.</exception>
    public ColorRadialGradient GetRadialGradient()
    {
        if (radialGradient == null)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a radial gradient.");
        }
        return radialGradient;
    }

    /// <summary>Gets resolved sweep-gradient geometry.</summary>
    /// <returns>The sweep-gradient geometry.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a sweep gradient.</exception>
    public ColorSweepGradient GetSweepGradient()
    {
        if (sweepGradient == null)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a sweep gradient.");
        }
        return sweepGradient;
    }

    /// <summary>Gets the encoded COLR extend mode of a gradient.</summary>
    /// <returns>Zero for pad, one for repeat, or two for reflect.</returns>
    /// <exception cref="System.InvalidOperationException">This paint has no gradient stops.</exception>
    public int GetExtendMode()
    {
        if (stops == null)
        {
            throw new System.InvalidOperationException(
                "This color paint has no gradient stops.");
        }

        return extendMode;
    }

    /// <summary>Gets the strongly typed COLR extend mode of a gradient.</summary>
    /// <returns>The gradient extension mode.</returns>
    /// <exception cref="System.InvalidOperationException">This paint has no gradient stops.</exception>
    public ColorGradientExtendMode GetGradientExtendMode()
    {
        return (ColorGradientExtendMode)GetExtendMode();
    }

    /// <summary>Gets the number of gradient stops.</summary>
    /// <returns>The gradient-stop count.</returns>
    public int GetStopCount() { return stops == null ? 0 : stops.Length; }

    /// <summary>Gets one gradient stop.</summary>
    /// <param name="index">The zero-based stop index.</param>
    /// <returns>The gradient stop.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the gradient.</exception>
    public ColorPaintStop GetStop(int index)
    {
        if (stops == null || index < 0 || index >= stops.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stops[index];
    }

    /// <summary>Gets the encoded COLR composite mode.</summary>
    /// <returns>The composite mode value.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a composite paint.</exception>
    public int GetCompositeMode()
    {
        if (kind != ColorPaintKind.Composite)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a composite.");
        }

        return compositeMode;
    }

    /// <summary>Gets the strongly typed COLR composite operation.</summary>
    /// <returns>The composite operation.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a composite paint.</exception>
    public ColorCompositeMode GetCompositeOperation()
    {
        return (ColorCompositeMode)GetCompositeMode();
    }

    /// <summary>Gets the source paint of a composite.</summary>
    /// <returns>The source paint.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a composite paint.</exception>
    public ColorPaint GetCompositeSource()
    {
        if (kind != ColorPaintKind.Composite)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a composite.");
        }
        return children[0];
    }

    /// <summary>Gets the backdrop paint of a composite.</summary>
    /// <returns>The backdrop paint.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a composite paint.</exception>
    public ColorPaint GetCompositeBackdrop()
    {
        if (kind != ColorPaintKind.Composite)
        {
            throw new System.InvalidOperationException(
                "This color paint is not a composite.");
        }
        return children[1];
    }
}
