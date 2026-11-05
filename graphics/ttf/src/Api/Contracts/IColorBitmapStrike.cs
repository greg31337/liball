namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ColorBitmapStrike"/>.</summary>
public interface IColorBitmapStrike
{
    /// <summary>Gets the bitmap table representation.</summary>
    /// <returns>The <c>sbix</c>, CBDT, or EBDT representation.</returns>
    ColorGlyphRepresentation GetRepresentation();

    /// <summary>Gets the glyph requested from this strike.</summary>
    /// <returns>The requested glyph identifier.</returns>
    int GetGlyphId();

    /// <summary>Gets the first glyph identifier covered by the strike.</summary>
    /// <returns>The first glyph identifier.</returns>
    int GetStartGlyphId();

    /// <summary>Gets the last glyph identifier covered by the strike.</summary>
    /// <returns>The last glyph identifier.</returns>
    int GetEndGlyphId();

    /// <summary>Gets the horizontal pixels-per-em size.</summary>
    /// <returns>The horizontal strike size.</returns>
    int GetPixelsPerEmX();

    /// <summary>Gets the vertical pixels-per-em size.</summary>
    /// <returns>The vertical strike size.</returns>
    int GetPixelsPerEmY();

    /// <summary>Gets the recorded pixels-per-inch value.</summary>
    /// <returns>The <c>sbix</c> resolution, or zero when the format does not store one.</returns>
    int GetPixelsPerInch();

    /// <summary>Gets the bitmap bit depth.</summary>
    /// <returns>The embedded-bitmap depth, or zero for <c>sbix</c>.</returns>
    int GetBitDepth();

    /// <summary>Gets the glyph that supplied the resolved image bytes.</summary>
    /// <returns>The source glyph identifier after <c>sbix</c> duplicate resolution.</returns>
    int GetSourceGlyphId();

    /// <summary>Gets the horizontal <c>sbix</c> origin adjustment.</summary>
    /// <returns>The horizontal adjustment, or zero for other formats.</returns>
    int GetOriginOffsetX();

    /// <summary>Gets the vertical <c>sbix</c> origin adjustment.</summary>
    /// <returns>The vertical adjustment, or zero for other formats.</returns>
    int GetOriginOffsetY();

    /// <summary>Gets the source image-format value.</summary>
    /// <returns>An <c>sbix</c> graphic tag or an embedded-bitmap image format.</returns>
    int GetImageFormat();

    /// <summary>Gets the format-specific payload kind.</summary>
    /// <returns>The interpreted payload kind.</returns>
    ColorBitmapPayloadKind GetPayloadKind();

    /// <summary>Reports whether the payload kind can be rendered from exposed data.</summary>
    /// <returns>True for a supported payload; otherwise false.</returns>
    bool IsSupported();

    /// <summary>Reports whether the payload is a complete PNG image.</summary>
    /// <returns>True for PNG payloads.</returns>
    bool IsPng();

    /// <summary>Reports whether the payload uses an image compression format.</summary>
    /// <returns>True for PNG, JPEG, or TIFF payloads.</returns>
    bool IsCompressed();

    /// <summary>Reports whether the ordinary outline must accompany the image.</summary>
    /// <returns>True when the <c>sbix</c> draw-outline flag is set.</returns>
    bool ShouldDrawOutline();

    /// <summary>Gets the glyph metrics stored with the embedded bitmap.</summary>
    /// <returns>The metrics, or null for <c>sbix</c> or unsupported records.</returns>
    ColorBitmapGlyphMetrics? GetGlyphMetrics();

    /// <summary>Gets the strike's horizontal line metrics.</summary>
    /// <returns>The metrics, or null for <c>sbix</c>.</returns>
    ColorBitmapLineMetrics? GetHorizontalLineMetrics();

    /// <summary>Gets the strike's vertical line metrics.</summary>
    /// <returns>The metrics, or null for <c>sbix</c>.</returns>
    ColorBitmapLineMetrics? GetVerticalLineMetrics();

    /// <summary>Gets the number of composite bitmap components.</summary>
    /// <returns>The component count.</returns>
    int GetComponentCount();

    /// <summary>Gets one composite bitmap component.</summary>
    /// <param name="index">The zero-based component index.</param>
    /// <returns>The component.</returns>
    ColorBitmapComponent GetComponent(int index);

    /// <summary>Copies the resolved image or bitmap payload.</summary>
    /// <returns>A new byte array.</returns>
    byte[] CopyData();
}
