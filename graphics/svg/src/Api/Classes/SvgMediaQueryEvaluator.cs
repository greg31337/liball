namespace svg;

/// <summary>Evaluates parsed media query lists against a concrete style environment.</summary>
public class SvgMediaQueryEvaluator : ISvgMediaQueryEvaluator
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgMediaQueryEvaluator()
    {
    }

    /// <summary>Tests whether any valid query in a list matches an environment.</summary>
    /// <param name="queryList">The parsed media query list.</param>
    /// <param name="environment">The style environment.</param>
    /// <returns><c>true</c> when at least one query matches.</returns>
    public bool Matches(SvgMediaQueryList? queryList, SvgStyleEnvironment? environment)
    {
        if (queryList == null || !queryList.IsValid || environment == null || !new SvgMediaQueryEvaluator().ValidEnvironment(environment)) return false;
        int index = 0;
        while (index < queryList.Queries.Count)
        {
            if (new SvgMediaQueryEvaluator().MatchesQuery(queryList.Queries[index], environment)) return true;
            index = index + 1;
        }
        return false;
    }
bool MatchesQuery(SvgMediaQuery? query, SvgStyleEnvironment environment)
    {
        if (query == null || !query.IsValid) return false;
        bool matches = query.MediaType == "all" || string.Equals(query.MediaType, environment.MediaType, System.StringComparison.OrdinalIgnoreCase);
        int index = 0;
        while (matches && index < query.Features.Count)
        {
            matches = new SvgMediaQueryEvaluator().MatchesFeature(query.Features[index], environment);
            index = index + 1;
        }
        return query.Modifier == SvgMediaQueryModifier.Not ? !matches : matches;
    }
bool MatchesFeature(SvgMediaFeature? feature, SvgStyleEnvironment environment)
    {
        if (feature == null || !feature.IsValid || feature.Kind == SvgMediaFeatureKind.Unknown) return false;
        if (feature.Comparison == SvgMediaComparison.Boolean) return new SvgMediaQueryEvaluator().BooleanValue(feature.Kind, environment);
        if (feature.Kind == SvgMediaFeatureKind.Orientation)
        {
            string orientation = environment.ViewportWidth > environment.ViewportHeight ? "landscape" : "portrait";
            return feature.Comparison == SvgMediaComparison.Equal && feature.Keyword == orientation;
        }
        if (feature.Kind == SvgMediaFeatureKind.ReducedMotion)
        {
            string preference = environment.ReducedMotion ? "reduce" : "no-preference";
            return feature.Comparison == SvgMediaComparison.Equal && feature.Keyword == preference;
        }
        double actual = new SvgMediaQueryEvaluator().NumericEnvironmentValue(feature.Kind, environment);
        double expected = new SvgMediaQueryEvaluator().NumericFeatureValue(feature, environment);
        if (!SvgFiniteMath.IsFinite(actual) || !SvgFiniteMath.IsFinite(expected)) return false;
        return new SvgMediaQueryEvaluator().Compare(actual, expected, feature.Comparison);
    }
bool BooleanValue(SvgMediaFeatureKind kind, SvgStyleEnvironment environment)
    {
        if (kind == SvgMediaFeatureKind.Width) return environment.ViewportWidth > 0;
        if (kind == SvgMediaFeatureKind.Height) return environment.ViewportHeight > 0;
        if (kind == SvgMediaFeatureKind.Resolution) return environment.ResolutionDpi > 0;
        if (kind == SvgMediaFeatureKind.Color) return environment.ColorBits > 0;
        if (kind == SvgMediaFeatureKind.Monochrome) return environment.MonochromeBits > 0;
        if (kind == SvgMediaFeatureKind.ReducedMotion) return environment.ReducedMotion;
        return kind == SvgMediaFeatureKind.Orientation;
    }
double NumericEnvironmentValue(SvgMediaFeatureKind kind, SvgStyleEnvironment environment)
    {
        if (kind == SvgMediaFeatureKind.Width) return environment.ViewportWidth;
        if (kind == SvgMediaFeatureKind.Height) return environment.ViewportHeight;
        if (kind == SvgMediaFeatureKind.Resolution) return environment.ResolutionDpi;
        if (kind == SvgMediaFeatureKind.Color) return environment.ColorBits;
        if (kind == SvgMediaFeatureKind.Monochrome) return environment.MonochromeBits;
        return double.NaN;
    }
double NumericFeatureValue(SvgMediaFeature feature, SvgStyleEnvironment environment)
    {
        if (feature.Kind == SvgMediaFeatureKind.Width || feature.Kind == SvgMediaFeatureKind.Height) return new SvgMediaQueryEvaluator().LengthPixels(feature.NumericValue, feature.Unit, environment);
        if (feature.Kind == SvgMediaFeatureKind.Resolution)
        {
            if (feature.Unit == "dpcm") return feature.NumericValue * 2.54;
            if (feature.Unit == "dppx") return feature.NumericValue * 96;
        }
        return feature.NumericValue;
    }
double LengthPixels(double value, string unit, SvgStyleEnvironment environment)
    {
        if (unit == "px") return value;
        if (unit == "in") return value * 96;
        if (unit == "cm") return value * 96 / 2.54;
        if (unit == "mm") return value * 96 / 25.4;
        if (unit == "q") return value * 96 / 101.6;
        if (unit == "pt") return value * 96 / 72;
        if (unit == "pc") return value * 16;
        if (unit == "em" || unit == "rem") return value * environment.RootFontSize;
        if (unit == "vw") return value * environment.ViewportWidth / 100;
        if (unit == "vh") return value * environment.ViewportHeight / 100;
        if (unit == "vmin") return value * System.Math.Min(environment.ViewportWidth, environment.ViewportHeight) / 100;
        if (unit == "vmax") return value * System.Math.Max(environment.ViewportWidth, environment.ViewportHeight) / 100;
        return double.NaN;
    }
bool Compare(double actual, double expected, SvgMediaComparison comparison)
    {
        if (comparison == SvgMediaComparison.Equal) return actual == expected;
        if (comparison == SvgMediaComparison.MinimumInclusive) return actual >= expected;
        if (comparison == SvgMediaComparison.MinimumExclusive) return actual > expected;
        if (comparison == SvgMediaComparison.MaximumInclusive) return actual <= expected;
        if (comparison == SvgMediaComparison.MaximumExclusive) return actual < expected;
        return false;
    }
bool ValidEnvironment(SvgStyleEnvironment environment)
    {
        if (environment.MediaType == null || environment.MediaType.Length == 0) return false;
        if (!SvgFiniteMath.IsFinite(environment.ViewportWidth) || environment.ViewportWidth < 0) return false;
        if (!SvgFiniteMath.IsFinite(environment.ViewportHeight) || environment.ViewportHeight < 0) return false;
        if (!SvgFiniteMath.IsFinite(environment.ResolutionDpi) || environment.ResolutionDpi <= 0) return false;
        if (environment.ColorBits < 0 || environment.MonochromeBits < 0) return false;
        return SvgFiniteMath.IsFinite(environment.RootFontSize) && environment.RootFontSize > 0;
    }
}
