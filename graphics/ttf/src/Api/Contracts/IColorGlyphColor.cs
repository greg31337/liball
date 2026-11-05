namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ColorGlyphColor"/>.</summary>
public interface IColorGlyphColor
{
    /// <summary>Gets the red component.</summary>
    /// <returns>The red component from zero through 255.</returns>
    int GetRed();

    /// <summary>Gets the green component.</summary>
    /// <returns>The green component from zero through 255.</returns>
    int GetGreen();

    /// <summary>Gets the blue component.</summary>
    /// <returns>The blue component from zero through 255.</returns>
    int GetBlue();

    /// <summary>Gets the alpha component.</summary>
    /// <returns>The alpha component from zero through 255.</returns>
    int GetAlpha();
}
