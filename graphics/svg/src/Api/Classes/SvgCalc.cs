namespace svg;

/// <summary>Parses CSS <c>calc()</c>, <c>min()</c>, <c>max()</c>, and <c>clamp()</c> expressions.</summary>
public class SvgCalcParser : ISvgCalcParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgCalcParser()
    {
    }

    /// <summary>Parses CSS math without custom properties.</summary>
    /// <param name="value">The CSS math text.</param>
    /// <param name="expectedDimension">The permitted result dimension.</param>
    /// <param name="maxDepth">The maximum expression depth.</param>
    /// <param name="maxNodes">The maximum expression-node count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A typed expression or stable failure.</returns>
    public SvgCalcParseResult ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters(string value, SvgCalcDimension expectedDimension, int maxDepth, int maxNodes, int maxTokenCharacters)
    {
        return new SvgCalcParser().ParseWithValueAndPropertiesAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters(value, new SvgCustomPropertySet(), expectedDimension, maxDepth, maxNodes, maxTokenCharacters);
    }

    /// <summary>Substitutes custom properties and parses typed CSS math.</summary>
    /// <param name="value">The CSS math text.</param>
    /// <param name="properties">The custom-property table.</param>
    /// <param name="expectedDimension">The permitted result dimension.</param>
    /// <param name="maxDepth">The maximum substitution and expression depth.</param>
    /// <param name="maxNodes">The maximum expression-node count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A substituted typed expression or stable failure.</returns>
    public SvgCalcParseResult ParseWithValueAndPropertiesAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters(string? value, SvgCustomPropertySet? properties, SvgCalcDimension expectedDimension, int maxDepth, int maxNodes, int maxTokenCharacters)
    {
        System.Collections.Generic.List<string> empty = new System.Collections.Generic.List<string>();
        if (value == null || properties == null || !new SvgCalcParser().IsExpectedDimension(expectedDimension) || maxDepth < 1 || maxNodes < 1 || maxTokenCharacters < 1) return new SvgCalcParseResult(false, SvgCalcParseError.InvalidArgument, SvgCssVariableError.None, null, "", empty, 0);
        int maximumExpansionTokens = maxNodes > int.MaxValue / 8 ? int.MaxValue : maxNodes * 8;
        SvgCssVariableResolveResult resolved = new SvgCssVariableResolver().Resolve(properties, value, maxDepth, maximumExpansionTokens, maxTokenCharacters);
        if (!resolved.Success)
        {
            SvgCalcParseError error = resolved.Error == SvgCssVariableError.TokenTooLong ? SvgCalcParseError.TokenTooLong : SvgCalcParseError.VariableFailure;
            return new SvgCalcParseResult(false, error, resolved.Error, null, "", resolved.Dependencies, resolved.ErrorOffset);
        }
        SvgCalcParserState state = new SvgCalcParserState(resolved.Text, maxDepth, maxNodes, maxTokenCharacters);
        SvgCalcWorkResult parsed = state.ParseComplete();
        if (!parsed.Success) return new SvgCalcParseResult(false, parsed.Error, SvgCssVariableError.None, null, resolved.Text, resolved.Dependencies, parsed.ErrorOffset);
        if (!new SvgCalcParser().IsExpectedResult(expectedDimension, (parsed.Expression ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Dimension)) return new SvgCalcParseResult(false, SvgCalcParseError.IncompatibleDimensions, SvgCssVariableError.None, parsed.Expression, resolved.Text, resolved.Dependencies, parsed.Expression.StartOffset);
        return new SvgCalcParseResult(true, SvgCalcParseError.None, SvgCssVariableError.None, parsed.Expression, resolved.Text, resolved.Dependencies, -1);
    }
bool IsExpectedDimension(SvgCalcDimension dimension)
    {
        return dimension >= SvgCalcDimension.Any && dimension <= SvgCalcDimension.Resolution;
    }
bool IsExpectedResult(SvgCalcDimension expected, SvgCalcDimension actual)
    {
        if (expected == SvgCalcDimension.Any || expected == actual) return true;
        return expected == SvgCalcDimension.LengthPercentage && (actual == SvgCalcDimension.Length || actual == SvgCalcDimension.Percentage || actual == SvgCalcDimension.LengthPercentage);
    }
}
