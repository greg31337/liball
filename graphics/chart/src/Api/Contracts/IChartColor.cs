namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartColor"/>.</summary>
public interface IChartColor
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartColor Copy();
    /// <summary>
    /// Compares all four color channels with another color.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool EqualsColor(ChartColor color);
    /// <summary>
    /// Determines whether this value is opaque.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsOpaque();
    /// <summary>
    /// Determines whether this value is transparent.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsTransparent();
    /// <summary>
    /// Creates a copy with the specified alpha.
    /// </summary>
    /// <param name = "alpha">The alpha channel from 0 for transparent through 255 for opaque.</param>
    /// <returns>The resulting color.</returns>
    ChartColor WithAlpha(byte alpha);
    /// <summary>
    /// Creates a copy with the specified opacity.
    /// </summary>
    /// <param name = "opacity">The opacity from zero for transparent through one for opaque.</param>
    /// <returns>The resulting color.</returns>
    ChartColor WithOpacity(double opacity);
    /// <summary>
    /// Scales the red, green, and blue channels by the alpha fraction.
    /// </summary>
    /// <returns>The resulting color.</returns>
    ChartColor PremultiplyAlpha();
    /// <summary>
    /// Restores straight color channels from a premultiplied-alpha color.
    /// </summary>
    /// <returns>The resulting color.</returns>
    ChartColor UnpremultiplyAlpha();
    /// <summary>
    /// Converts this value to hex rgb.
    /// </summary>
    /// <returns>The converted string.</returns>
    string ToHexRgb();
    /// <summary>
    /// Converts this value to hex rgba.
    /// </summary>
    /// <returns>The converted string.</returns>
    string ToHexRgba();
    /// <summary>Returns the current r.</summary>
    /// <returns>The current field value.</returns>
    byte GetR();
    /// <summary>Replaces the current r.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetR(byte value);
    /// <summary>Returns the current g.</summary>
    /// <returns>The current field value.</returns>
    byte GetG();
    /// <summary>Replaces the current g.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetG(byte value);
    /// <summary>Returns the current b.</summary>
    /// <returns>The current field value.</returns>
    byte GetB();
    /// <summary>Replaces the current b.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetB(byte value);
    /// <summary>Returns the current a.</summary>
    /// <returns>The current field value.</returns>
    byte GetA();
    /// <summary>Replaces the current a.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetA(byte value);
}
