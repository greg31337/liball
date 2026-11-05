namespace ttf;

/// <summary>Stores an eight-bit red, green, blue, and alpha color.</summary>
public class ColorGlyphColor : IColorGlyphColor
{
    int red;
    int green;
    int blue;
    int alpha;

    /// <summary>Creates an RGBA color.</summary>
    /// <param name="red">The red component from zero through 255.</param>
    /// <param name="green">The green component from zero through 255.</param>
    /// <param name="blue">The blue component from zero through 255.</param>
    /// <param name="alpha">The alpha component from zero through 255.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// A component is outside the range from zero through 255.
    /// </exception>
    public ColorGlyphColor(int red, int green, int blue, int alpha)
    {
        ValidateComponent(red, "red");
        ValidateComponent(green, "green");
        ValidateComponent(blue, "blue");
        ValidateComponent(alpha, "alpha");
        this.red = red;
        this.green = green;
        this.blue = blue;
        this.alpha = alpha;
    }

    /// <summary>Gets the red component.</summary>
    /// <returns>The red component from zero through 255.</returns>
    public int GetRed() { return red; }

    /// <summary>Gets the green component.</summary>
    /// <returns>The green component from zero through 255.</returns>
    public int GetGreen() { return green; }

    /// <summary>Gets the blue component.</summary>
    /// <returns>The blue component from zero through 255.</returns>
    public int GetBlue() { return blue; }

    /// <summary>Gets the alpha component.</summary>
    /// <returns>The alpha component from zero through 255.</returns>
    public int GetAlpha() { return alpha; }

    internal SfntCpalColor ToCpalColor()
    {
        return new SfntCpalColor(blue, green, red, alpha);
    }

    static void ValidateComponent(int value, string parameterName)
    {
        if (value < 0 || value > 255)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}
