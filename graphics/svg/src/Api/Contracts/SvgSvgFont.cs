namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgSvgFontReader"/>.</summary>
public interface ISvgSvgFontReader
{
    /// <summary>Reads one legacy SVG font element with secure defaults.</summary>
    /// <param name="fontNode">The legacy font element.</param>
    /// <returns>The parsed font or stable diagnostics.</returns>
    SvgSvgFontReadResult ReadWithFontNode(SvgNode fontNode);
    /// <summary>Reads one legacy SVG font element with explicit bounds.</summary>
    /// <param name="fontNode">The legacy font element.</param>
    /// <param name="options">The glyph, kerning, path, token, and cancellation bounds.</param>
    /// <returns>The parsed font or stable diagnostics.</returns>
    SvgSvgFontReadResult ReadWithFontNodeAndOptions(SvgNode? fontNode, SvgSvgFontOptions options);
}
