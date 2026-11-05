using System;

namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPaintParser"/>.</summary>
public interface ISvgPaintParser
{
    /// <summary>Parses a complete SVG paint value with optional invalid-fallback recovery.</summary>
    /// <param name="value">The SVG paint text.</param>
    /// <param name="maxFallbacks">The maximum encountered fallback count.</param>
    /// <param name="maxTokenCharacters">The maximum URL, keyword, or color token length.</param>
    /// <param name="recoverInvalidFallbacks">Whether malformed fallbacks are skipped.</param>
    /// <returns>A complete, recovered, or failed paint parse result.</returns>
    SvgPaintParseResult Parse(string? value, int maxFallbacks, int maxTokenCharacters, bool recoverInvalidFallbacks);
}
