namespace gif;

/// <summary>Stores one color with eight-bit red, green, blue, and alpha channels.</summary>
public class GifRgbaColor
{
    /// <summary>Contains the red channel.</summary>
    public byte Red;
    /// <summary>Contains the green channel.</summary>
    public byte Green;
    /// <summary>Contains the blue channel.</summary>
    public byte Blue;
    /// <summary>Contains the alpha channel.</summary>
    public byte Alpha;

    /// <summary>Creates a color from eight-bit red, green, blue, and alpha channels.</summary>
    /// <param name="red">The red channel.</param>
    /// <param name="green">The green channel.</param>
    /// <param name="blue">The blue channel.</param>
    /// <param name="alpha">The alpha channel.</param>
    public GifRgbaColor(byte red, byte green, byte blue, byte alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }
}
