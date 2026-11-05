using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxColorOperations"/>.
/// </summary>
public interface IPptxColorOperations
{
    /// <summary>
    /// Creates a new color using the supplied values.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting color value.</returns>
    PptxColor CreateRgb(string value);
    /// <summary>
    /// Creates a new color using the supplied values.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <returns>The resulting color value.</returns>
    PptxColor CreateScheme(string value);
    /// <summary>
    /// Creates a DrawingML preset color.
    /// </summary>
    /// <param name="value">The preset color name.</param>
    /// <returns>The resulting color value.</returns>
    PptxColor CreatePreset(string value);
    /// <summary>
    /// Creates a system color with an optional last-known RGB fallback.
    /// </summary>
    /// <param name="value">The system color name.</param>
    /// <param name="fallbackRgb">The last-known RGB fallback, or an empty string.</param>
    /// <returns>The resulting color value.</returns>
    PptxColor CreateSystem(string value, string fallbackRgb);
    /// <summary>
    /// Creates a color using hue, saturation, and luminance components.
    /// </summary>
    /// <param name="hueDegrees">The hue in degrees.</param>
    /// <param name="saturation">The saturation from zero through 100,000.</param>
    /// <param name="luminance">The luminance from zero through 100,000.</param>
    /// <returns>The resulting color value.</returns>
    PptxColor CreateHsl(double hueDegrees, int saturation, int luminance);
    /// <summary>
    /// Reads a DrawingML color element and its transforms.
    /// </summary>
    /// <param name="xml">The color element XML.</param>
    /// <returns>The parsed color value.</returns>
    PptxColor ParseXml(string xml);
    /// <summary>
    /// Writes a DrawingML color element and its transforms.
    /// </summary>
    /// <param name="color">The color value to write.</param>
    /// <returns>The serialized color XML.</returns>
    string ToXml(PptxColor color);
    /// <summary>
    /// Resolves a non-scheme color and its transforms to RGB.
    /// </summary>
    /// <param name="color">The color value to resolve.</param>
    /// <returns>The effective RGB color.</returns>
    PptxColor Resolve(PptxColor color);
}
