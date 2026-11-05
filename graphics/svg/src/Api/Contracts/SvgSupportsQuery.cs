namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgSupportsQueryParser"/>.</summary>
public interface ISvgSupportsQueryParser
{
    /// <summary>Parses an anonymous supports condition.</summary>
    /// <param name="value">The complete condition text.</param>
    /// <param name="limits">The CSS parsing limits.</param>
    /// <returns>The parsed condition and diagnostics.</returns>
    SvgSupportsParseResult ParseWithValueAndLimits(string value, SvgCssLimits limits);
    /// <summary>Parses a supports condition with source identity.</summary>
    /// <param name="value">The complete condition text.</param>
    /// <param name="limits">The CSS parsing limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed condition and diagnostics.</returns>
    SvgSupportsParseResult ParseWithValueAndLimitsAndSource(string value, SvgCssLimits limits, SvgSourceUri source);
}

/// <summary>Defines the public operations exposed by <see cref="SvgSupportsQueryEvaluator"/>.</summary>
public interface ISvgSupportsQueryEvaluator
{
    /// <summary>Evaluates a parsed supports query.</summary>
    /// <param name="query">The parsed supports query.</param>
    /// <param name="environment">The property and capability environment.</param>
    /// <param name="limits">The CSS parsing limits.</param>
    /// <returns><c>true</c> when the valid query evaluates true.</returns>
    bool Matches(SvgSupportsQuery? query, SvgSupportsEnvironment? environment, SvgCssLimits? limits);
}
