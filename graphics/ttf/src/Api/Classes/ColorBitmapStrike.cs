namespace ttf;

/// <summary>Describes one resolved bitmap strike for a glyph.</summary>
public class ColorBitmapStrike : IColorBitmapStrike
{
    ColorGlyphRepresentation representation;
    int glyphId;
    int startGlyphId;
    int endGlyphId;
    int pixelsPerEmX;
    int pixelsPerEmY;
    int pixelsPerInch;
    int bitDepth;
    int sourceGlyphId;
    int originOffsetX;
    int originOffsetY;
    int imageFormat;
    ColorBitmapPayloadKind payloadKind;
    bool supported;
    bool drawOutline;
    ColorBitmapGlyphMetrics? glyphMetrics;
    ColorBitmapLineMetrics? horizontalLineMetrics;
    ColorBitmapLineMetrics? verticalLineMetrics;
    ColorBitmapComponent[] components;
    byte[] data;

    internal ColorBitmapStrike(
        ColorGlyphRepresentation representation,
        int glyphId,
        int startGlyphId,
        int endGlyphId,
        int pixelsPerEmX,
        int pixelsPerEmY,
        int pixelsPerInch,
        int bitDepth,
        int sourceGlyphId,
        int originOffsetX,
        int originOffsetY,
        int imageFormat,
        ColorBitmapPayloadKind payloadKind,
        bool supported,
        bool drawOutline,
        ColorBitmapGlyphMetrics? glyphMetrics,
        ColorBitmapLineMetrics? horizontalLineMetrics,
        ColorBitmapLineMetrics? verticalLineMetrics,
        ColorBitmapComponent[] components,
        byte[] data)
    {
        this.representation = representation;
        this.glyphId = glyphId;
        this.startGlyphId = startGlyphId;
        this.endGlyphId = endGlyphId;
        this.pixelsPerEmX = pixelsPerEmX;
        this.pixelsPerEmY = pixelsPerEmY;
        this.pixelsPerInch = pixelsPerInch;
        this.bitDepth = bitDepth;
        this.sourceGlyphId = sourceGlyphId;
        this.originOffsetX = originOffsetX;
        this.originOffsetY = originOffsetY;
        this.imageFormat = imageFormat;
        this.payloadKind = payloadKind;
        this.supported = supported;
        this.drawOutline = drawOutline;
        this.glyphMetrics = glyphMetrics;
        this.horizontalLineMetrics = horizontalLineMetrics;
        this.verticalLineMetrics = verticalLineMetrics;
        this.components = components;
        this.data = data;
    }

    /// <summary>Gets the bitmap table representation.</summary>
    /// <returns>The <c>sbix</c>, CBDT, or EBDT representation.</returns>
    public ColorGlyphRepresentation GetRepresentation() { return representation; }

    /// <summary>Gets the glyph requested from this strike.</summary>
    /// <returns>The requested glyph identifier.</returns>
    public int GetGlyphId() { return glyphId; }

    /// <summary>Gets the first glyph identifier covered by the strike.</summary>
    /// <returns>The first glyph identifier.</returns>
    public int GetStartGlyphId() { return startGlyphId; }

    /// <summary>Gets the last glyph identifier covered by the strike.</summary>
    /// <returns>The last glyph identifier.</returns>
    public int GetEndGlyphId() { return endGlyphId; }

    /// <summary>Gets the horizontal pixels-per-em size.</summary>
    /// <returns>The horizontal strike size.</returns>
    public int GetPixelsPerEmX() { return pixelsPerEmX; }

    /// <summary>Gets the vertical pixels-per-em size.</summary>
    /// <returns>The vertical strike size.</returns>
    public int GetPixelsPerEmY() { return pixelsPerEmY; }

    /// <summary>Gets the recorded pixels-per-inch value.</summary>
    /// <returns>The <c>sbix</c> resolution, or zero when the format does not store one.</returns>
    public int GetPixelsPerInch() { return pixelsPerInch; }

    /// <summary>Gets the bitmap bit depth.</summary>
    /// <returns>The embedded-bitmap depth, or zero for <c>sbix</c>.</returns>
    public int GetBitDepth() { return bitDepth; }

    /// <summary>Gets the glyph that supplied the resolved image bytes.</summary>
    /// <returns>The source glyph identifier after <c>sbix</c> duplicate resolution.</returns>
    public int GetSourceGlyphId() { return sourceGlyphId; }

    /// <summary>Gets the horizontal <c>sbix</c> origin adjustment.</summary>
    /// <returns>The horizontal adjustment, or zero for other formats.</returns>
    public int GetOriginOffsetX() { return originOffsetX; }

    /// <summary>Gets the vertical <c>sbix</c> origin adjustment.</summary>
    /// <returns>The vertical adjustment, or zero for other formats.</returns>
    public int GetOriginOffsetY() { return originOffsetY; }

    /// <summary>Gets the source image-format value.</summary>
    /// <returns>An <c>sbix</c> graphic tag or an embedded-bitmap image format.</returns>
    public int GetImageFormat() { return imageFormat; }

    /// <summary>Gets the format-specific payload kind.</summary>
    /// <returns>The interpreted payload kind.</returns>
    public ColorBitmapPayloadKind GetPayloadKind() { return payloadKind; }

    /// <summary>Reports whether the payload kind can be rendered from exposed data.</summary>
    /// <returns>True for a supported payload; otherwise false.</returns>
    public bool IsSupported() { return supported; }

    /// <summary>Reports whether the payload is a complete PNG image.</summary>
    /// <returns>True for PNG payloads.</returns>
    public bool IsPng() { return payloadKind == ColorBitmapPayloadKind.Png; }

    /// <summary>Reports whether the payload uses an image compression format.</summary>
    /// <returns>True for PNG, JPEG, or TIFF payloads.</returns>
    public bool IsCompressed()
    {
        return payloadKind == ColorBitmapPayloadKind.Png ||
            payloadKind == ColorBitmapPayloadKind.Jpeg ||
            payloadKind == ColorBitmapPayloadKind.Tiff;
    }

    /// <summary>Reports whether the ordinary outline must accompany the image.</summary>
    /// <returns>True when the <c>sbix</c> draw-outline flag is set.</returns>
    public bool ShouldDrawOutline() { return drawOutline; }

    /// <summary>Gets the glyph metrics stored with the embedded bitmap.</summary>
    /// <returns>The metrics, or null for <c>sbix</c> or unsupported records.</returns>
    public ColorBitmapGlyphMetrics? GetGlyphMetrics() { return glyphMetrics; }

    /// <summary>Gets the strike's horizontal line metrics.</summary>
    /// <returns>The metrics, or null for <c>sbix</c>.</returns>
    public ColorBitmapLineMetrics? GetHorizontalLineMetrics()
    {
        return horizontalLineMetrics;
    }

    /// <summary>Gets the strike's vertical line metrics.</summary>
    /// <returns>The metrics, or null for <c>sbix</c>.</returns>
    public ColorBitmapLineMetrics? GetVerticalLineMetrics()
    {
        return verticalLineMetrics;
    }

    /// <summary>Gets the number of composite bitmap components.</summary>
    /// <returns>The component count.</returns>
    public int GetComponentCount() { return components.Length; }

    /// <summary>Gets one composite bitmap component.</summary>
    /// <param name="index">The zero-based component index.</param>
    /// <returns>The component.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public ColorBitmapComponent GetComponent(int index)
    {
        if (index < 0 || index >= components.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return components[index];
    }

    /// <summary>Copies the resolved image or bitmap payload.</summary>
    /// <returns>A new byte array.</returns>
    public byte[] CopyData() { return data.AsSpan().ToArray(); }
}
