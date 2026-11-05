namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ColorPaint"/>.</summary>
public interface IColorPaint
{
    /// <summary>Gets this node's normalized operation.</summary>
    /// <returns>The paint kind.</returns>
    ColorPaintKind GetKind();

    /// <summary>Gets the source COLR paint format.</summary>
    /// <returns>The encoded paint format, or zero for a COLR version zero node.</returns>
    int GetSourceFormat();

    /// <summary>Gets the strongly typed source COLR paint format.</summary>
    /// <returns>The format, or unsupported when the encoded format is unknown.</returns>
    ColorPaintFormat GetFormat();

    /// <summary>Gets the number of child paints.</summary>
    /// <returns>The child count.</returns>
    int GetChildCount();

    /// <summary>Gets one child paint.</summary>
    /// <param name="index">The zero-based child index.</param>
    /// <returns>The child paint.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the node.</exception>
    ColorPaint GetChild(int index);

    /// <summary>Gets the number of paints in a layer-list node.</summary>
    /// <returns>The layer count.</returns>
    int GetLayerCount();

    /// <summary>Gets one paint from a layer-list node in paint order.</summary>
    /// <param name="index">The zero-based layer index.</param>
    /// <returns>The layer paint.</returns>
    ColorPaint GetLayer(int index);

    /// <summary>Gets the glyph identifier referenced by a glyph paint.</summary>
    /// <returns>The referenced glyph identifier.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a glyph paint.</exception>
    int GetGlyphId();

    /// <summary>Gets the fill paint applied by a glyph paint.</summary>
    /// <returns>The glyph fill paint.</returns>
    ColorPaint GetGlyphPaint();

    /// <summary>Gets the expanded paint referenced by a COLR-glyph paint.</summary>
    /// <returns>The referenced glyph's normalized paint.</returns>
    ColorPaint GetReferencedPaint();

    /// <summary>Gets the CPAL entry used by a palette-color paint.</summary>
    /// <returns>The zero-based palette entry index.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a palette-color paint.</exception>
    int GetPaletteEntryIndex();

    /// <summary>Gets the resolved RGBA color of a color-reference paint.</summary>
    /// <returns>The selected palette or foreground color.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a color-reference paint.</exception>
    ColorGlyphColor GetColor();

    /// <summary>Gets the paint alpha multiplier of a color-reference paint.</summary>
    /// <returns>The alpha multiplier.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a color-reference paint.</exception>
    double GetAlpha();

    /// <summary>Gets the number of normalized numeric values on this paint.</summary>
    /// <returns>The value count.</returns>
    int GetValueCount();

    /// <summary>Gets one normalized numeric paint value.</summary>
    /// <param name="index">The zero-based value index.</param>
    /// <returns>The numeric value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the node.</exception>
    double GetValue(int index);

    /// <summary>Gets the resolved affine matrix of a transform paint.</summary>
    /// <returns>The normalized affine transform.</returns>
    ColorAffineTransform GetTransform();

    /// <summary>Gets the child affected by a transform paint.</summary>
    /// <returns>The transformed child paint.</returns>
    ColorPaint GetTransformedPaint();

    /// <summary>Gets resolved linear-gradient geometry.</summary>
    /// <returns>The linear-gradient geometry.</returns>
    ColorLinearGradient GetLinearGradient();

    /// <summary>Gets resolved radial-gradient geometry.</summary>
    /// <returns>The radial-gradient geometry.</returns>
    ColorRadialGradient GetRadialGradient();

    /// <summary>Gets resolved sweep-gradient geometry.</summary>
    /// <returns>The sweep-gradient geometry.</returns>
    ColorSweepGradient GetSweepGradient();

    /// <summary>Gets the encoded COLR extend mode of a gradient.</summary>
    /// <returns>Zero for pad, one for repeat, or two for reflect.</returns>
    /// <exception cref="System.InvalidOperationException">This paint has no gradient stops.</exception>
    int GetExtendMode();

    /// <summary>Gets the strongly typed COLR extend mode of a gradient.</summary>
    /// <returns>The gradient extension mode.</returns>
    ColorGradientExtendMode GetGradientExtendMode();

    /// <summary>Gets the number of gradient stops.</summary>
    /// <returns>The gradient-stop count.</returns>
    int GetStopCount();

    /// <summary>Gets one gradient stop.</summary>
    /// <param name="index">The zero-based stop index.</param>
    /// <returns>The gradient stop.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is outside the gradient.</exception>
    ColorPaintStop GetStop(int index);

    /// <summary>Gets the encoded COLR composite mode.</summary>
    /// <returns>The composite mode value.</returns>
    /// <exception cref="System.InvalidOperationException">This is not a composite paint.</exception>
    int GetCompositeMode();

    /// <summary>Gets the strongly typed COLR composite operation.</summary>
    /// <returns>The composite operation.</returns>
    ColorCompositeMode GetCompositeOperation();

    /// <summary>Gets the source paint of a composite.</summary>
    /// <returns>The source paint.</returns>
    ColorPaint GetCompositeSource();

    /// <summary>Gets the backdrop paint of a composite.</summary>
    /// <returns>The backdrop paint.</returns>
    ColorPaint GetCompositeBackdrop();
}
