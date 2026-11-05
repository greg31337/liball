namespace ttf;

/// <summary>Defines one component of a composite embedded bitmap.</summary>
public interface IColorBitmapComponent
{
    /// <summary>Gets the component glyph identifier.</summary>
    /// <returns>The glyph identifier.</returns>
    int GetGlyphId();
    /// <summary>Gets the component x offset in pixels.</summary>
    /// <returns>The x offset.</returns>
    int GetXOffset();
    /// <summary>Gets the component y offset in pixels.</summary>
    /// <returns>The y offset.</returns>
    int GetYOffset();
}
