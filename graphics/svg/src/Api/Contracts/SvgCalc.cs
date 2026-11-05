namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCalcParser"/>.</summary>
public interface ISvgCalcParser
{
    /// <summary>Parses CSS math without custom properties.</summary>
    /// <param name="value">The CSS math text.</param>
    /// <param name="expectedDimension">The permitted result dimension.</param>
    /// <param name="maxDepth">The maximum expression depth.</param>
    /// <param name="maxNodes">The maximum expression-node count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A typed expression or stable failure.</returns>
    SvgCalcParseResult ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters(string value, SvgCalcDimension expectedDimension, int maxDepth, int maxNodes, int maxTokenCharacters);
    /// <summary>Substitutes custom properties and parses typed CSS math.</summary>
    /// <param name="value">The CSS math text.</param>
    /// <param name="properties">The custom-property table.</param>
    /// <param name="expectedDimension">The permitted result dimension.</param>
    /// <param name="maxDepth">The maximum substitution and expression depth.</param>
    /// <param name="maxNodes">The maximum expression-node count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A substituted typed expression or stable failure.</returns>
    SvgCalcParseResult ParseWithValueAndPropertiesAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters(string? value, SvgCustomPropertySet? properties, SvgCalcDimension expectedDimension, int maxDepth, int maxNodes, int maxTokenCharacters);
}
