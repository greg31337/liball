using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxThemeOperations"/>.
/// </summary>
public interface IPptxThemeOperations
{
    /// <summary>
    /// Returns the color without changing the theme.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting color value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxColor GetColor(PptxTheme target, string name);
    /// <summary>
    /// Returns the color value without changing the theme.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetColorValue(PptxTheme target, string name);
    /// <summary>
    /// Replaces a color in an existing theme color slot.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    /// <param name="name">The theme color slot name.</param>
    /// <param name="color">The replacement color.</param>
    void SetColor(PptxTheme target, string name, PptxColor color);
    /// <summary>
    /// Resolves a theme-based color to its effective RGB color.
    /// </summary>
    /// <param name="color">The color value to use.</param>
    /// <returns>The resulting color value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxColor ResolveColor(PptxTheme target, PptxColor color);
    /// <summary>
    /// Returns the font without changing the theme.
    /// </summary>
    /// <param name="role">The role value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetFont(PptxTheme target, string role);
    /// <summary>
    /// Fills style count with the supplied values.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int FillStyleCount(PptxTheme target);
    /// <summary>
    /// Returns the number of style entries currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int LineStyleCount(PptxTheme target);
    /// <summary>
    /// Returns the number of style entries currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int EffectStyleCount(PptxTheme target);
    /// <summary>
    /// Returns the number of fill style entries currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int BackgroundFillStyleCount(PptxTheme target);
    /// <summary>
    /// Returns the fill style without changing the theme.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting fill value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxFill GetFillStyle(PptxTheme target, int index);
    /// <summary>
    /// Returns the line style without changing the theme.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting line value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxLine GetLineStyle(PptxTheme target, int index);
    /// <summary>
    /// Returns the effect style without changing the theme.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting effect value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxEffect GetEffectStyle(PptxTheme target, int index);
    /// <summary>
    /// Returns the background fill style without changing the theme.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting fill value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxFill GetBackgroundFillStyle(PptxTheme target, int index);
}
