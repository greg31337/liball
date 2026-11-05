namespace ttf;

/// <summary>Represents one component of a composite embedded bitmap.</summary>
public class ColorBitmapComponent : IColorBitmapComponent
{
    int glyphId;
    int xOffset;
    int yOffset;

    internal ColorBitmapComponent(int glyphId, int xOffset, int yOffset)
    {
        this.glyphId = glyphId;
        this.xOffset = xOffset;
        this.yOffset = yOffset;
    }

    /// <summary>Gets the component glyph identifier.</summary>
    /// <returns>The glyph identifier.</returns>
    public int GetGlyphId() { return glyphId; }
    /// <summary>Gets the component x offset in pixels.</summary>
    /// <returns>The x offset.</returns>
    public int GetXOffset() { return xOffset; }
    /// <summary>Gets the component y offset in pixels.</summary>
    /// <returns>The y offset.</returns>
    public int GetYOffset() { return yOffset; }
}
