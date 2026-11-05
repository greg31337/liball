namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgInlineTextLayout"/>.</summary>
public interface ISvgInlineTextLayout
{
    /// <summary>Processes text using wrapping horizontal defaults.</summary>
    /// <param name="text">The UTF-16 source text.</param>
    /// <returns>The normalized items and lines.</returns>
    SvgInlineTextResult LayoutWithText(string text);
    /// <summary>Processes text using explicit white-space and layout options.</summary>
    /// <param name="text">The UTF-16 source text.</param>
    /// <param name="options">The white-space, writing-mode, and sizing options.</param>
    /// <returns>The normalized items and lines.</returns>
    SvgInlineTextResult LayoutWithStringAndSvgInlineTextOptions(string text, SvgInlineTextOptions options);
    /// <summary>Processes a previously normalized Unicode text model.</summary>
    /// <param name="text">The normalized Unicode text model.</param>
    /// <param name="options">The white-space, writing-mode, and sizing options.</param>
    /// <returns>The normalized items and lines.</returns>
    SvgInlineTextResult LayoutWithSvgUnicodeTextAndSvgInlineTextOptions(SvgUnicodeText text, SvgInlineTextOptions options);
}
