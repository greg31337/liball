namespace gif;

/// <summary>Stores one opaque RGB entry in a GIF color table.</summary>
public class GifColor
{
    /// <summary>Contains the red channel from 0 through 255.</summary>
    public byte Red;
    /// <summary>Contains the green channel from 0 through 255.</summary>
    public byte Green;
    /// <summary>Contains the blue channel from 0 through 255.</summary>
    public byte Blue;

    /// <summary>Creates a color entry from eight-bit red, green, and blue channels.</summary>
    /// <param name="red">The red channel.</param>
    /// <param name="green">The green channel.</param>
    /// <param name="blue">The blue channel.</param>
    public GifColor(byte red, byte green, byte blue)
    {
        Red = red;
        Green = green;
        Blue = blue;
    }
}

/// <summary>Stores the ordered RGB entries referenced by indexed GIF pixels.</summary>
/// <remarks>A color table written to GIF must have a power-of-two entry count from 2 through 256. The array is not cloned by the constructor.</remarks>
public class GifPalette
{
    /// <summary>Contains palette entries in index order.</summary>
    public GifColor[] Colors;
    /// <summary>Contains the active entry count, normally equal to the length of <see cref="Colors"/>.</summary>
    public int Count;

    /// <summary>Creates a palette over the supplied entry array.</summary>
    /// <param name="colors">The colors in palette-index order; <c>null</c> creates an empty palette.</param>
    public GifPalette(GifColor[]? colors)
    {
        if (colors == null)
        {
            Colors = new GifColor[0];
            Count = 0;
        }
        else
        {
            Colors = colors;
            Count = colors.Length;
        }
    }

    internal bool IsEmpty()
    {
        return Count == 0;
    }
}
