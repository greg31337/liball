namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCssParser"/>.</summary>
public interface ISvgCssParser
{
    /// <summary>Parses an anonymous author-origin stylesheet.</summary>
    /// <param name="value">The complete CSS source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The parsed stylesheet and recovery diagnostics.</returns>
    SvgCssParseResult ParseStyleSheetWithValueAndLimits(string value, SvgCssLimits limits);
    /// <summary>Parses a stylesheet with explicit origin and source identity.</summary>
    /// <param name="value">The complete CSS source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="origin">The cascade origin.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed stylesheet and recovery diagnostics.</returns>
    SvgCssParseResult ParseStyleSheetWithValueAndLimitsAndOriginAndSource(string value, SvgCssLimits limits, SvgStyleOrigin origin, SvgSourceUri source);
    /// <summary>Parses an anonymous standalone declaration list.</summary>
    /// <param name="value">The declaration-list source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The parsed declarations and recovery diagnostics.</returns>
    SvgCssParseResult ParseDeclarationListWithValueAndLimits(string value, SvgCssLimits limits);
    /// <summary>Parses a standalone declaration list with source identity.</summary>
    /// <param name="value">The declaration-list source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed declarations and recovery diagnostics.</returns>
    SvgCssParseResult ParseDeclarationListWithValueAndLimitsAndSource(string value, SvgCssLimits limits, SvgSourceUri source);
    /// <summary>Parses an anonymous style attribute.</summary>
    /// <param name="value">The style attribute source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The parsed declarations and recovery diagnostics.</returns>
    SvgCssParseResult ParseStyleAttributeWithValueAndLimits(string value, SvgCssLimits limits);
    /// <summary>Parses a style attribute with source identity.</summary>
    /// <param name="value">The style attribute source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed declarations and recovery diagnostics.</returns>
    SvgCssParseResult ParseStyleAttributeWithValueAndLimitsAndSource(string value, SvgCssLimits limits, SvgSourceUri source);
    /// <summary>Parses an anonymous selector list.</summary>
    /// <param name="value">The selector source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The parsed selector list or located failure.</returns>
    SvgSelectorParseResult ParseSelectorWithValueAndLimits(string value, SvgCssLimits limits);
    /// <summary>Parses a selector list with source identity.</summary>
    /// <param name="value">The selector source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed selector list or located failure.</returns>
    SvgSelectorParseResult ParseSelectorWithValueAndLimitsAndSource(string value, SvgCssLimits limits, SvgSourceUri source);
}
