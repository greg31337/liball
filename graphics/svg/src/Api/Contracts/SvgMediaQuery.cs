namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgMediaQueryParser"/>.</summary>
public interface ISvgMediaQueryParser
{
    /// <summary>Parses an anonymous media query list.</summary>
    /// <param name="value">The complete media query list.</param>
    /// <param name="limits">The CSS token and character limits.</param>
    /// <returns>The parsed list and located diagnostics.</returns>
    SvgMediaQueryParseResult ParseWithValueAndLimits(string value, SvgCssLimits limits);
    /// <summary>Parses a media query list with source identity.</summary>
    /// <param name="value">The complete media query list.</param>
    /// <param name="limits">The CSS token and character limits.</param>
    /// <param name="source">The source resource.</param>
    /// <returns>The parsed list and located diagnostics.</returns>
    SvgMediaQueryParseResult ParseWithValueAndLimitsAndSource(string value, SvgCssLimits limits, SvgSourceUri source);
}

/// <summary>Defines the public operations exposed by <see cref="SvgMediaQueryEvaluator"/>.</summary>
public interface ISvgMediaQueryEvaluator
{
    /// <summary>Tests whether any valid query in a list matches an environment.</summary>
    /// <param name="queryList">The parsed media query list.</param>
    /// <param name="environment">The style environment.</param>
    /// <returns><c>true</c> when at least one query matches.</returns>
    bool Matches(SvgMediaQueryList? queryList, SvgStyleEnvironment? environment);
}
