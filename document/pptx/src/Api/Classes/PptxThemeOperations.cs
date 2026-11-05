using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTheme values.
/// </summary>
public class PptxThemeOperations : IPptxThemeOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxThemeOperations()
    {
    }

    /// <summary>
    /// Returns the color without changing the theme.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting color value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxColor GetColor(PptxTheme target, string name)
    {
        return target.GetColor(name);
    }

    /// <summary>
    /// Returns the color value without changing the theme.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetColorValue(PptxTheme target, string name)
    {
        return target.GetColorValue(name);
    }

    /// <summary>
    /// Replaces a color in an existing theme color slot.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    /// <param name="name">The theme color slot name.</param>
    /// <param name="color">The replacement color.</param>
    public void SetColor(PptxTheme target, string name, PptxColor color)
    {
        target.SetColor(name, color);
    }

    /// <summary>
    /// Resolves a theme-based color to its effective RGB color.
    /// </summary>
    /// <param name="color">The color value to use.</param>
    /// <returns>The resulting color value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxColor ResolveColor(PptxTheme target, PptxColor color)
    {
        return target.ResolveColor(color);
    }

    /// <summary>
    /// Returns the font without changing the theme.
    /// </summary>
    /// <param name="role">The role value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetFont(PptxTheme target, string role)
    {
        return target.GetFont(role);
    }

    /// <summary>
    /// Fills style count with the supplied values.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int FillStyleCount(PptxTheme target)
    {
        return target.FillStyleCount();
    }

    /// <summary>
    /// Returns the number of style entries currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int LineStyleCount(PptxTheme target)
    {
        return target.LineStyleCount();
    }

    /// <summary>
    /// Returns the number of style entries currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int EffectStyleCount(PptxTheme target)
    {
        return target.EffectStyleCount();
    }

    /// <summary>
    /// Returns the number of fill style entries currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int BackgroundFillStyleCount(PptxTheme target)
    {
        return target.BackgroundFillStyleCount();
    }

    /// <summary>
    /// Returns the fill style without changing the theme.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting fill value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxFill GetFillStyle(PptxTheme target, int index)
    {
        return target.GetFillStyle(index);
    }

    /// <summary>
    /// Returns the line style without changing the theme.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting line value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxLine GetLineStyle(PptxTheme target, int index)
    {
        return target.GetLineStyle(index);
    }

    /// <summary>
    /// Returns the effect style without changing the theme.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting effect value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxEffect GetEffectStyle(PptxTheme target, int index)
    {
        return target.GetEffectStyle(index);
    }

    /// <summary>
    /// Returns the background fill style without changing the theme.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting fill value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxFill GetBackgroundFillStyle(PptxTheme target, int index)
    {
        return target.GetBackgroundFillStyle(index);
    }
}
