using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxStyleResolver values.
/// </summary>
public class PptxStyleResolver : IPptxStyleResolver
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxStyleResolver()
    {
    }

    /// <summary>
    /// Resolves a theme-based color and its tint or shade transforms.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="color">The color value to use.</param>
    /// <returns>The resulting color value.</returns>
    public PptxColor ResolveColor(PptxTheme theme, PptxColor color)
    {
        return PptxStyleResolverCore.ResolveColor(theme, color);
    }

    /// <summary>
    /// Returns a copy of the fill with theme colors resolved.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="fill">The fill value to use.</param>
    /// <returns>The resulting fill value.</returns>
    public PptxFill ResolveFill(PptxTheme theme, PptxFill fill)
    {
        return PptxStyleResolverCore.ResolveFill(theme, fill);
    }

    /// <summary>
    /// Returns a copy of the line with theme colors resolved.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="line">The line value to use.</param>
    /// <returns>The resulting line value.</returns>
    public PptxLine ResolveLine(PptxTheme theme, PptxLine line)
    {
        return PptxStyleResolverCore.ResolveLine(theme, line);
    }

    /// <summary>
    /// Returns the shape fill with theme colors resolved.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <returns>The resulting fill value.</returns>
    public PptxFill ResolveShapeFill(PptxTheme theme, PptxShape shape)
    {
        return PptxStyleResolverCore.ResolveShapeFill(theme, shape);
    }

    /// <summary>
    /// Returns the shape line with theme colors resolved.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <returns>The resulting line value.</returns>
    public PptxLine ResolveShapeLine(PptxTheme theme, PptxShape shape)
    {
        return PptxStyleResolverCore.ResolveShapeLine(theme, shape);
    }

    /// <summary>
    /// Resolves the requested major or minor theme font.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <param name="role">The role value to use.</param>
    /// <returns>The resulting text.</returns>
    public string ResolveTextFont(PptxTheme theme, string role)
    {
        return PptxStyleResolverCore.ResolveTextFont(theme, role);
    }

    /// <summary>
    /// Resolves the theme text color.
    /// </summary>
    /// <param name="theme">The theme value to use.</param>
    /// <returns>The resulting color value.</returns>
    public PptxColor ResolveTextColor(PptxTheme theme)
    {
        return PptxStyleResolverCore.ResolveTextColor(theme);
    }
}
