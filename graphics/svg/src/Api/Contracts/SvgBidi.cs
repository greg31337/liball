namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgBidiAnalyzer"/>.</summary>
public interface ISvgBidiAnalyzer
{
    /// <summary>Analyzes text using automatic paragraph direction.</summary>
    /// <param name="text">The UTF-16 logical text.</param>
    /// <returns>The resolved bidirectional data and visual mappings.</returns>
    SvgBidiResult AnalyzeWithText(string text);
    /// <summary>Analyzes text using explicit bounds and paragraph direction.</summary>
    /// <param name="text">The UTF-16 logical text.</param>
    /// <param name="options">The analysis bounds and direction.</param>
    /// <returns>The resolved bidirectional data and visual mappings.</returns>
    SvgBidiResult AnalyzeWithStringAndSvgBidiOptions(string text, SvgBidiOptions options);
    /// <summary>Analyzes a previously normalized Unicode text model.</summary>
    /// <param name="text">The validated normalized Unicode text model.</param>
    /// <param name="options">The analysis bounds and direction.</param>
    /// <returns>The resolved bidirectional data and visual mappings.</returns>
    SvgBidiResult AnalyzeWithSvgUnicodeTextAndSvgBidiOptions(SvgUnicodeText text, SvgBidiOptions options);
}
