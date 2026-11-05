namespace chart;
/// <summary>Defines substitutable chart color factory operations.</summary>
public interface IChartColorFactory
{
    /// <summary>
    /// Creates an opaque color from red, green, and blue byte channels.
    /// </summary>
    /// <param name = "r">The red channel from 0 through 255.</param>
    /// <param name = "g">The green channel from 0 through 255.</param>
    /// <param name = "b">The blue channel from 0 through 255.</param>
    /// <returns>The resulting color.</returns>
    ChartColor FromRgb(byte r, byte g, byte b);
    /// <summary>
    /// Creates a color from red, green, blue, and alpha byte channels.
    /// </summary>
    /// <param name = "r">The red channel from 0 through 255.</param>
    /// <param name = "g">The green channel from 0 through 255.</param>
    /// <param name = "b">The blue channel from 0 through 255.</param>
    /// <param name = "a">The alpha channel from 0 for transparent through 255 for opaque.</param>
    /// <returns>The resulting color.</returns>
    ChartColor FromRgba(byte r, byte g, byte b, byte a);
    /// <summary>
    /// Creates a fully transparent black color.
    /// </summary>
    /// <returns>The resulting color.</returns>
    ChartColor Transparent();
    /// <summary>
    /// Creates an opaque black color.
    /// </summary>
    /// <returns>The resulting color.</returns>
    ChartColor Black();
    /// <summary>
    /// Creates an opaque white color.
    /// </summary>
    /// <returns>The resulting color.</returns>
    ChartColor White();
    /// <summary>
    /// Applies the lerp operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "start">The start.</param>
    /// <param name = "end">The end.</param>
    /// <param name = "amount">The amount.</param>
    /// <returns>The resulting color.</returns>
    ChartColor Lerp(ChartColor start, ChartColor end, double amount);
    /// <summary>
    /// Parses a named, hexadecimal, RGB, or RGBA color and rejects unsupported text.
    /// </summary>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <returns>The resulting color.</returns>
    /// <exception cref = "System.FormatException">Thrown when the input text does not use a supported format.</exception>
    ChartColor Parse(string text);
    /// <summary>
    /// Parses a named, hexadecimal, RGB, or RGBA color when the text is supported.
    /// </summary>
    /// <param name = "text">The text to parse, or <c>null</c> when no color is available.</param>
    /// <returns>The parsed value, or <c>null</c> when the input is not supported.</returns>
    ChartColor? TryParse(string? text);
}
