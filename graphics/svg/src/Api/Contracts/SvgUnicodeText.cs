namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgUnicodeTextAnalyzer"/>.</summary>
public interface ISvgUnicodeTextAnalyzer
{
    /// <summary>Analyzes text using secure bounds and the undetermined language.</summary>
    /// <param name="text">The UTF-16 source text.</param>
    /// <returns>The normalized text and logical mappings.</returns>
    SvgUnicodeTextResult AnalyzeWithText(string text);
    /// <summary>Analyzes text using explicit bounds and language spans.</summary>
    /// <param name="text">The UTF-16 source text.</param>
    /// <param name="options">The bounds, languages, and cancellation token.</param>
    /// <returns>The normalized text and logical mappings.</returns>
    SvgUnicodeTextResult AnalyzeWithTextAndOptions(string text, SvgUnicodeTextOptions options);
}
