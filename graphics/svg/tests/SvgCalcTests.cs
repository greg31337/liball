namespace svg.Tests;

public class SvgCalcTests
{
    [Test]
    public void CustomPropertiesStoreBalancedSourcePreservingTokens()
    {
        SvgCustomPropertySet properties = new SvgCustomPropertySet();
        string raw = " calc(10px + 2%) /* keep */ \"x,y\" ";
        SvgCustomPropertyStoreResult stored = properties.Set("--Size", raw, 32, 64);
        SvgCustomPropertyStoreResult replaced = properties.Set("--Size", "5px", 8, 16);
        SvgCustomPropertyStoreResult distinctCase = properties.Set("--size", "6px", 8, 16);

        Assert.Multiple(() =>
        {
            Assert.That(stored.Success, Is.True);
            Assert.That(string.Concat(stored.Value.Tokens.Select(token => token.Text)), Is.EqualTo(raw));
            Assert.That(stored.Value.Tokens.Any(token => token.Kind == SvgCssStoredTokenKind.Comment), Is.True);
            Assert.That(replaced.Success, Is.True);
            Assert.That(properties.Get("--Size").RawText, Is.EqualTo("5px"));
            Assert.That(properties.Get("--size").RawText, Is.EqualTo("6px"));
            Assert.That(properties.GetCount(), Is.EqualTo(2));
            Assert.That(properties.Remove("--Size"), Is.True);
            Assert.That(properties.GetCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void CustomPropertyStorageRejectsNamesMalformedTokensAndBounds()
    {
        SvgCustomPropertySet properties = new SvgCustomPropertySet();
        SvgCustomPropertyStoreResult name = properties.Set("size", "1px", 8, 16);
        SvgCustomPropertyStoreResult brackets = properties.Set("--x", "calc(1px", 8, 16);
        SvgCustomPropertyStoreResult stringValue = properties.Set("--x", "\"open", 8, 16);
        SvgCustomPropertyStoreResult tokenLength = properties.Set("--x", "12345", 8, 4);
        SvgCustomPropertyStoreResult tokenCount = properties.Set("--x", "1 + 2", 2, 16);

        Assert.Multiple(() =>
        {
            Assert.That(name.Error, Is.EqualTo(SvgCssVariableError.InvalidName));
            Assert.That(brackets.Error, Is.EqualTo(SvgCssVariableError.InvalidTokens));
            Assert.That(stringValue.Error, Is.EqualTo(SvgCssVariableError.InvalidTokens));
            Assert.That(tokenLength.Error, Is.EqualTo(SvgCssVariableError.TokenTooLong));
            Assert.That(tokenCount.Error, Is.EqualTo(SvgCssVariableError.TokenLimitExceeded));
            Assert.That(properties.GetCount(), Is.Zero);
        });
    }

    [Test]
    public void VariableResolutionSupportsNestedValuesFallbacksAndDependencies()
    {
        SvgCustomPropertySet properties = new SvgCustomPropertySet();
        properties.Set("--base", "4px", 8, 16);
        properties.Set("--double", "calc(var(--base) * 2)", 16, 32);

        SvgCssVariableResolveResult nested = new SvgCssVariableResolver().Resolve(properties, "var(--double)", 8, 64, 32);
        SvgCssVariableResolveResult fallback = new SvgCssVariableResolver().Resolve(properties, "var(--missing, var(--base))", 8, 64, 32);
        SvgCssVariableResolveResult emptyFallback = new SvgCssVariableResolver().Resolve(properties, "a var(--missing,) b", 8, 64, 32);

        Assert.Multiple(() =>
        {
            Assert.That(nested.Success, Is.True);
            Assert.That(nested.Text, Is.EqualTo("calc(4px * 2)"));
            Assert.That(nested.Dependencies, Is.EqualTo(new[] { "--double", "--base" }));
            Assert.That(fallback.Text, Is.EqualTo(" 4px"));
            Assert.That(fallback.Dependencies, Is.EqualTo(new[] { "--missing", "--base" }));
            Assert.That(emptyFallback.Text, Is.EqualTo("a  b"));
        });
    }

    [Test]
    public void VariableResolutionDetectsCyclesAndAllowsConsumerFallbacks()
    {
        SvgCustomPropertySet properties = new SvgCustomPropertySet();
        properties.Set("--a", "var(--b)", 8, 32);
        properties.Set("--b", "var(--a)", 8, 32);
        properties.Set("--self", "var(--self, 7px)", 8, 32);

        SvgCssVariableResolveResult cycle = new SvgCssVariableResolver().Resolve(properties, "var(--a)", 8, 64, 32);
        SvgCssVariableResolveResult fallback = new SvgCssVariableResolver().Resolve(properties, "var(--a, 5px)", 8, 64, 32);
        SvgCssVariableResolveResult selfCycle = new SvgCssVariableResolver().Resolve(properties, "var(--self)", 8, 64, 32);
        SvgCssVariableResolveResult selfFallback = new SvgCssVariableResolver().Resolve(properties, "var(--self, 3px)", 8, 64, 32);

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Success, Is.False);
            Assert.That(cycle.Error, Is.EqualTo(SvgCssVariableError.CyclicVariable));
            Assert.That(cycle.Dependencies, Is.EqualTo(new[] { "--a", "--b" }));
            Assert.That(fallback.Success, Is.True);
            Assert.That(fallback.Text, Is.EqualTo(" 5px"));
            Assert.That(selfCycle.Error, Is.EqualTo(SvgCssVariableError.CyclicVariable));
            Assert.That(selfFallback.Success, Is.True);
            Assert.That(selfFallback.Text, Is.EqualTo(" 3px"));
        });
    }

    [Test]
    public void VariableResolutionEnforcesDepthAndExpansionBounds()
    {
        SvgCustomPropertySet properties = new SvgCustomPropertySet();
        properties.Set("--a", "var(--b)", 8, 32);
        properties.Set("--b", "var(--c)", 8, 32);
        properties.Set("--c", "1px", 8, 32);
        properties.Set("--many", "1 2 3", 8, 32);

        SvgCssVariableResolveResult depth = new SvgCssVariableResolver().Resolve(properties, "var(--a)", 2, 32, 32);
        SvgCssVariableResolveResult depthFallback = new SvgCssVariableResolver().Resolve(properties, "var(--a, 5px)", 2, 32, 32);
        SvgCssVariableResolveResult expansion = new SvgCssVariableResolver().Resolve(properties, "var(--many)", 8, 2, 32);
        SvgCssVariableResolveResult missing = new SvgCssVariableResolver().Resolve(properties, "var(--missing)", 8, 32, 32);

        Assert.Multiple(() =>
        {
            Assert.That(depth.Error, Is.EqualTo(SvgCssVariableError.DepthExceeded));
            Assert.That(depthFallback.Error, Is.EqualTo(SvgCssVariableError.DepthExceeded));
            Assert.That(expansion.Error, Is.EqualTo(SvgCssVariableError.ExpansionLimitExceeded));
            Assert.That(missing.Error, Is.EqualTo(SvgCssVariableError.MissingVariable));
        });
    }

    [Test]
    public void CalcParserBuildsPrecedenceAwareTypedExpressionTrees()
    {
        SvgCalcParseResult result = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1px + 2 * 3px - 4px / 2)", SvgCalcDimension.Length, 16, 64, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Expression.Kind, Is.EqualTo(SvgCalcExpressionKind.Calc));
            Assert.That(result.Expression.Dimension, Is.EqualTo(SvgCalcDimension.Length));
            Assert.That(result.Expression.Children[0].Kind, Is.EqualTo(SvgCalcExpressionKind.Subtract));
            Assert.That(result.Expression.Children[0].Children[0].Kind, Is.EqualTo(SvgCalcExpressionKind.Add));
            Assert.That(result.Expression.Children[0].Children[0].Children[1].Kind, Is.EqualTo(SvgCalcExpressionKind.Multiply));
            Assert.That(result.Expression.Children[0].Children[1].Kind, Is.EqualTo(SvgCalcExpressionKind.Divide));
        });
    }

    [Test]
    public void MinMaxAndClampValidateCompatibleDimensions()
    {
        SvgCalcParseResult minimum = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("min(10px, 5%, calc(2px + 3%))", SvgCalcDimension.LengthPercentage, 16, 64, 32);
        SvgCalcParseResult maximum = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("max(1s, 250ms)", SvgCalcDimension.Time, 16, 64, 32);
        SvgCalcParseResult clamp = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("clamp(0deg, 1turn, 400grad)", SvgCalcDimension.Angle, 16, 64, 32);

        Assert.Multiple(() =>
        {
            Assert.That(minimum.Success, Is.True);
            Assert.That(minimum.Expression.Kind, Is.EqualTo(SvgCalcExpressionKind.Min));
            Assert.That(minimum.Expression.Dimension, Is.EqualTo(SvgCalcDimension.LengthPercentage));
            Assert.That(maximum.Success, Is.True);
            Assert.That(maximum.Expression.Dimension, Is.EqualTo(SvgCalcDimension.Time));
            Assert.That(clamp.Success, Is.True);
            Assert.That(clamp.Expression.Kind, Is.EqualTo(SvgCalcExpressionKind.Clamp));
        });
    }

    [Test]
    public void CalcParserRecognizesEverySupportedUnitDimension()
    {
        SvgCalcParseResult length = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1em + 2vw)", SvgCalcDimension.Length, 8, 32, 16);
        SvgCalcParseResult percentage = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(25% + 10%)", SvgCalcDimension.Percentage, 8, 32, 16);
        SvgCalcParseResult frequency = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1kHz + 500Hz)", SvgCalcDimension.Frequency, 8, 32, 16);
        SvgCalcParseResult resolution = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("min(96dpi, 1dppx)", SvgCalcDimension.Resolution, 8, 32, 16);

        Assert.Multiple(() =>
        {
            Assert.That(length.Success, Is.True);
            Assert.That(percentage.Success, Is.True);
            Assert.That(frequency.Success, Is.True);
            Assert.That(resolution.Success, Is.True);
        });
    }

    [Test]
    public void CalcParserRejectsInvalidDimensionalAlgebraAndFunctionArities()
    {
        SvgCalcParseResult addition = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1px + 1s)", SvgCalcDimension.Any, 8, 32, 16);
        SvgCalcParseResult product = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1px * 2s)", SvgCalcDimension.Any, 8, 32, 16);
        SvgCalcParseResult dividedUnit = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1px / 2px)", SvgCalcDimension.Any, 8, 32, 16);
        SvgCalcParseResult zero = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1px / 0)", SvgCalcDimension.Any, 8, 32, 16);
        SvgCalcParseResult comparison = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("min(1deg, 1s)", SvgCalcDimension.Any, 8, 32, 16);
        SvgCalcParseResult clamp = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("clamp(1px, 2px)", SvgCalcDimension.Any, 8, 32, 16);

        Assert.Multiple(() =>
        {
            Assert.That(addition.Error, Is.EqualTo(SvgCalcParseError.IncompatibleDimensions));
            Assert.That(product.Error, Is.EqualTo(SvgCalcParseError.InvalidProduct));
            Assert.That(dividedUnit.Error, Is.EqualTo(SvgCalcParseError.InvalidProduct));
            Assert.That(zero.Error, Is.EqualTo(SvgCalcParseError.DivisionByZero));
            Assert.That(comparison.Error, Is.EqualTo(SvgCalcParseError.IncompatibleDimensions));
            Assert.That(clamp.Error, Is.EqualTo(SvgCalcParseError.InvalidFunction));
        });
    }

    [Test]
    public void CalcAdditionRequiresCssWhitespaceButCommentsProvideIt()
    {
        SvgCalcParseResult missingBefore = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1px+ 2px)", SvgCalcDimension.Length, 8, 32, 16);
        SvgCalcParseResult missingAfter = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1px +2px)", SvgCalcDimension.Length, 8, 32, 16);
        SvgCalcParseResult comments = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1px/**/+/**/2px)", SvgCalcDimension.Length, 8, 32, 16);

        Assert.Multiple(() =>
        {
            Assert.That(missingBefore.Error, Is.EqualTo(SvgCalcParseError.UnexpectedToken));
            Assert.That(missingAfter.Error, Is.EqualTo(SvgCalcParseError.UnexpectedToken));
            Assert.That(comments.Success, Is.True);
        });
    }

    [Test]
    public void VariablesSubstituteBeforeCalcParsingWithFallbackAndCycleHandling()
    {
        SvgCustomPropertySet properties = new SvgCustomPropertySet();
        properties.Set("--size", "10px", 8, 32);
        properties.Set("--a", "var(--b)", 8, 32);
        properties.Set("--b", "var(--a)", 8, 32);

        SvgCalcParseResult mixed = new SvgCalcParser().ParseWithValueAndPropertiesAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(var(--size) + var(--missing, 5%))", properties, SvgCalcDimension.LengthPercentage, 16, 64, 32);
        SvgCalcParseResult cycleFallback = new SvgCalcParser().ParseWithValueAndPropertiesAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(var(--a, 2px) + 1px)", properties, SvgCalcDimension.Length, 16, 64, 32);
        SvgCalcParseResult missing = new SvgCalcParser().ParseWithValueAndPropertiesAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(var(--none) + 1px)", properties, SvgCalcDimension.Length, 16, 64, 32);

        Assert.Multiple(() =>
        {
            Assert.That(mixed.Success, Is.True);
            Assert.That(mixed.Expression.Dimension, Is.EqualTo(SvgCalcDimension.LengthPercentage));
            Assert.That(mixed.Dependencies, Is.EqualTo(new[] { "--size", "--missing" }));
            Assert.That(cycleFallback.Success, Is.True);
            Assert.That(missing.Error, Is.EqualTo(SvgCalcParseError.VariableFailure));
            Assert.That(missing.VariableError, Is.EqualTo(SvgCssVariableError.MissingVariable));
        });
    }

    [Test]
    public void CalcParserEnforcesExpectedDimensionsUnitsDepthNodesAndTokens()
    {
        SvgCalcParseResult expected = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("1px", SvgCalcDimension.Time, 8, 32, 16);
        SvgCalcParseResult unit = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("1qu", SvgCalcDimension.Any, 8, 32, 16);
        SvgCalcParseResult depth = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(calc(calc(1)))", SvgCalcDimension.Number, 2, 32, 16);
        SvgCalcParseResult nodes = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("calc(1 + 2 + 3)", SvgCalcDimension.Number, 8, 3, 16);
        SvgCalcParseResult token = new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters("12345px", SvgCalcDimension.Length, 8, 32, 4);

        Assert.Multiple(() =>
        {
            Assert.That(expected.Error, Is.EqualTo(SvgCalcParseError.IncompatibleDimensions));
            Assert.That(unit.Error, Is.EqualTo(SvgCalcParseError.UnknownUnit));
            Assert.That(depth.Error, Is.EqualTo(SvgCalcParseError.DepthExceeded));
            Assert.That(nodes.Error, Is.EqualTo(SvgCalcParseError.NodeLimitExceeded));
            Assert.That(token.Error, Is.EqualTo(SvgCalcParseError.TokenTooLong));
        });
    }
}
