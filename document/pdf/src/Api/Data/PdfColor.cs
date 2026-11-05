using System;

namespace pdf;

/// <summary>
/// Stores a device gray, RGB, or CMYK color in the numeric form consumed by PDF drawing commands.
/// </summary>
public class PdfColor
{
    /// <summary>
    /// Stores the PDF device color-space name: GRAY, RGB, or CMYK.
    /// </summary>
    public string Mode;
    /// <summary>
    /// Stores the first normalized color channel, from zero through one.
    /// </summary>
    public double First;
    /// <summary>
    /// Stores the second normalized color channel, from zero through one.
    /// </summary>
    public double Second;
    /// <summary>
    /// Stores the third normalized color channel, from zero through one.
    /// </summary>
    public double Third;
    /// <summary>
    /// Stores the fourth normalized color channel, from zero through one.
    /// </summary>
    public double Fourth;

    /// <summary>
    /// Initializes a new color.
    /// </summary>
    /// <param name="mode">The mode to use for this operation.</param>
    /// <param name="first">The first to use for this operation.</param>
    /// <param name="second">The second to use for this operation.</param>
    /// <param name="third">The third to use for this operation.</param>
    /// <param name="fourth">The fourth to use for this operation.</param>
    public PdfColor(string mode, double first, double second, double third, double fourth)
    {
        Mode = mode;
        First = first;
        Second = second;
        Third = third;
        Fourth = fourth;
    }

    /// <summary>
    /// Creates and returns a color configured for RGB.
    /// </summary>
    /// <param name="red">The normalized red channel, from zero through one.</param>
    /// <param name="green">The normalized green channel, from zero through one.</param>
    /// <param name="blue">The normalized blue channel, from zero through one.</param>
    /// <returns>The resulting color.</returns>
    internal static PdfColor Rgb(double red, double green, double blue)
    {
        ValidateUnit(red, "red");
        ValidateUnit(green, "green");
        ValidateUnit(blue, "blue");
        return new PdfColor("RGB", red, green, blue, 0);
    }

    /// <summary>
    /// Creates and returns a color configured for gray.
    /// </summary>
    /// <param name="gray">The normalized gray channel, from zero for black through one for white.</param>
    /// <returns>The resulting color.</returns>
    internal static PdfColor Gray(double gray)
    {
        ValidateUnit(gray, "gray");
        return new PdfColor("GRAY", gray, 0, 0, 0);
    }

    /// <summary>
    /// Creates and returns a color configured for CMYK.
    /// </summary>
    /// <param name="cyan">The normalized cyan channel, from zero through one.</param>
    /// <param name="magenta">The normalized magenta channel, from zero through one.</param>
    /// <param name="yellow">The normalized yellow channel, from zero through one.</param>
    /// <param name="black">The normalized black channel, from zero through one.</param>
    /// <returns>The resulting color.</returns>
    internal static PdfColor Cmyk(double cyan, double magenta, double yellow, double black)
    {
        ValidateUnit(cyan, "cyan");
        ValidateUnit(magenta, "magenta");
        ValidateUnit(yellow, "yellow");
        ValidateUnit(black, "black");
        return new PdfColor("CMYK", cyan, magenta, yellow, black);
    }

    internal static void ValidateUnit(double value, string name)
    {
        if (value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(name, "Color channel values must be between 0 and 1.");
        }
    }
}
