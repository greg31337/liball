using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxStyleResolver"/>.
/// </summary>
public interface IPptxStyleResolver
{
    /// <summary>
    /// Resolves a theme-based color and its tint or shade transforms.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="color">The color value to use.</param>
    /// <returns>The resulting color value.</returns>
    PptxColor ResolveColor(PptxTheme theme, PptxColor color);
    /// <summary>
    /// Returns a copy of the fill with theme colors resolved.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="fill">The fill value to use.</param>
    /// <returns>The resulting fill value.</returns>
    PptxFill ResolveFill(PptxTheme theme, PptxFill fill);
    /// <summary>
    /// Returns a copy of the line with theme colors resolved.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="line">The line value to use.</param>
    /// <returns>The resulting line value.</returns>
    PptxLine ResolveLine(PptxTheme theme, PptxLine line);
    /// <summary>
    /// Returns the shape fill with theme colors resolved.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <returns>The resulting fill value.</returns>
    PptxFill ResolveShapeFill(PptxTheme theme, PptxShape shape);
    /// <summary>
    /// Returns the shape line with theme colors resolved.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <returns>The resulting line value.</returns>
    PptxLine ResolveShapeLine(PptxTheme theme, PptxShape shape);
    /// <summary>
    /// Resolves the requested major or minor theme font.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="role">The role value to use.</param>
    /// <returns>The resulting text.</returns>
    string ResolveTextFont(PptxTheme theme, string role);
    /// <summary>
    /// Resolves the theme text color.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <returns>The resulting color value.</returns>
    PptxColor ResolveTextColor(PptxTheme theme);
}
