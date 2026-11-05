namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgSelectorParser"/>.</summary>
public interface ISvgSelectorParser
{
    /// <summary>Parses an anonymous selector list.</summary>
    /// <param name="value">The selector source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <returns>The parsed selector list or located failure.</returns>
    SvgSelectorParseResult ParseWithValueAndLimits(string value, SvgCssLimits limits);
    /// <summary>Parses a selector list with source identity.</summary>
    /// <param name="value">The selector source.</param>
    /// <param name="limits">The CSS security limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed selector list or located failure.</returns>
    SvgSelectorParseResult ParseWithValueAndLimitsAndSource(string? value, SvgCssLimits? limits, SvgSourceUri? source);
}
