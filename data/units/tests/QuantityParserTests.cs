using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityParserTests
{
    [Test]
    public void ParseOrThrow_ParsesNumericValueAndUnitExpression()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();

        QuantityValue quantity = parser.ParseOrThrow("9.81 m/s2", registry);

        Assert.That(quantity.GetValue(), Is.EqualTo(9.81d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("(meter/(second^2))"));
        Assert.That(quantity.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2))), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesCompactQuantitySyntax()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();

        QuantityValue quantity = parser.ParseOrThrow("12.5km", registry);

        Assert.That(quantity.GetValue(), Is.EqualTo(12.5d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("kilometer"));
        Assert.That(quantity.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1000, 1)), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesCompactCultureSpecificQuantitySyntax()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();
        QuantityParseOptions options = QuantityParseOptions.CreateDefault().WithCulture(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));

        QuantityValue quantity = parser.ParseOrThrowWithOptions("1,5km", registry, options);

        Assert.That(quantity.GetValue(), Is.EqualTo(1.5d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("kilometer"));
    }

    [Test]
    public void ParseOrThrow_ParsesSignedAndExponentValues()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();

        QuantityValue negative = parser.ParseOrThrow("-1.25e3 kg", registry);
        QuantityValue positive = parser.ParseOrThrow("+2E-3 s", registry);

        Assert.That(negative.GetValue(), Is.EqualTo(-1250.0d));
        Assert.That(negative.GetUnit().GetId(), Is.EqualTo("kilogram"));
        Assert.That(positive.GetValue(), Is.EqualTo(0.002d));
        Assert.That(positive.GetUnit().GetId(), Is.EqualTo("second"));
    }

    [Test]
    public void ParseOrThrow_UsesConfiguredCulture()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();
        QuantityParseOptions options = QuantityParseOptions.CreateDefault().WithCulture(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));

        QuantityValue quantity = parser.ParseOrThrowWithOptions("1,5 m", registry, options);

        Assert.That(quantity.GetValue(), Is.EqualTo(1.5d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("meter"));
    }

    [Test]
    public void ParseOrThrow_AllowsLenientWhitespaceAroundQuantityAndUnitExpression()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();

        QuantityValue quantity = parser.ParseOrThrow(" \t 2.5   km / s \n ", registry);

        Assert.That(quantity.GetValue(), Is.EqualTo(2.5d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("(kilometer/second)"));
        Assert.That(quantity.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time())), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesExponentValueWithNestedCompoundUnitExpression()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();

        QuantityValue quantity = parser.ParseOrThrow(" +4.2E3   (kB/s)/(MB/s) ", registry);

        Assert.That(quantity.GetValue(), Is.EqualTo(4200.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("((kilobyte/second)/(megabyte/second))"));
        Assert.That(quantity.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1000)), Is.True);
        Assert.That(quantity.GetUnit().GetDimension().IsDimensionless(), Is.True);
    }

    [Test]
    public void ParseOrThrow_StrictModeRequiresWhitespaceAndRejectsOuterWhitespace()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();
        QuantityParseOptions options = QuantityParseOptions.CreateDefault().WithStrict(true);

        QuantityValue quantity = parser.ParseOrThrowWithOptions("1 m", registry, options);

        Assert.That(quantity.GetValue(), Is.EqualTo(1.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("meter"));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrowWithOptions("1m", registry, options));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrowWithOptions(" 1 m", registry, options));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrowWithOptions("1 m ", registry, options));
    }

    [Test]
    public void ParseOrThrow_AppliesQuantityKindNarrowing()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();
        QuantityParseOptions options = QuantityParseOptions.CreateDefault().WithQuantityKind(new StandardQuantityKinds().Length());

        QuantityValue quantity = parser.ParseOrThrowWithOptions("3 km", registry, options);

        Assert.That(quantity.GetValue(), Is.EqualTo(3.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("kilometer"));
        Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrowWithOptions("3 s", registry, options));
    }

    [Test]
    public void ParseOrThrow_RejectsAmbiguousUnitExpressionAfterValue()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateAmbiguousUnitRegistry();

        System.InvalidOperationException exception = Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("1 u", registry))!;

        Assert.That(exception.Message, Does.Contain("ambiguous"));
    }

    [Test]
    public void ParseOrThrow_RejectsInvalidInputs()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();

        Assert.Throws<System.ArgumentNullException>(() => parser.ParseOrThrow(null!, registry));
        Assert.Throws<System.ArgumentNullException>(() => parser.ParseOrThrow("1 m", null!));
        Assert.Throws<System.ArgumentNullException>(() => parser.ParseOrThrowWithOptions("1 m", registry, null!));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("m", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("1", registry));
        Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("1 unknown", registry));
    }

    [Test]
    public void ParseOrThrow_RejectsInvalidNestedCompoundUnitExpressionAfterValue()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();

        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("1.5 ((km*kg)/(ms^2))*", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("2.0 (kB/s)/(MB/)", registry));
        Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("3.0 (km*kday)/s", registry));
        Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("4.0 K*m", registry));
    }

    [Test]
    public void ParseOrThrow_RejectsCultureMismatchAndMalformedValues()
    {
        QuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();

        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("1,5 m", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("+ m", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow(". m", registry));
    }

    [Test]
    public void Parse_ReturnsSuccessfulPublicResultWithoutThrowing()
    {
        IQuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();

        QuantityParseResult result = parser.Parse("9.81 m/s2", registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetQuantity().GetValue(), Is.EqualTo(9.81d));
        Assert.That(result.GetQuantity().GetUnit().GetId(), Is.EqualTo("(meter/(second^2))"));
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(result.GetMessage(), Is.EqualTo(""));
        Assert.That(result.GetPosition(), Is.EqualTo(-1));
        Assert.That(result.GetCandidates(), Is.Empty);
    }

    [Test]
    public void Parse_ReturnsStableNumericAndUnitFailurePositions()
    {
        IQuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();

        QuantityParseResult malformedValue = parser.Parse("+ m", registry);
        QuantityParseResult malformedExponent = parser.Parse("1e+ m", registry);
        QuantityParseResult unknownUnit = parser.Parse("1.5 m/zzunit", registry);
        QuantityParseResult missingOptions = parser.ParseWithOptions("1 m", registry, null!);

        Assert.That(malformedValue.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(malformedValue.GetPosition(), Is.EqualTo(0));
        Assert.That(malformedExponent.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(malformedExponent.GetPosition(), Is.EqualTo(1));
        Assert.That(unknownUnit.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseUnknownUnit));
        Assert.That(unknownUnit.GetPosition(), Is.EqualTo(6));
        Assert.That(unknownUnit.GetMessage(), Does.Contain("zzunit"));
        Assert.That(missingOptions.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(missingOptions.GetPosition(), Is.EqualTo(0));
    }

    [Test]
    public void Parse_OffsetsAmbiguityCandidatesToQuantitySourcePosition()
    {
        IQuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateAmbiguousUnitRegistry();

        QuantityParseResult result = parser.Parse("12 u", registry);
        UnitDefinition[] candidates = result.GetCandidates();
        candidates[0] = candidates[1];

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseAmbiguousUnit));
        Assert.That(result.GetPosition(), Is.EqualTo(3));
        Assert.That(result.GetCandidates()[0].GetId(), Is.EqualTo("alpha"));
        Assert.That(result.GetCandidates()[1].GetId(), Is.EqualTo("beta"));
        Assert.Throws<System.InvalidOperationException>(() => result.GetQuantity());
    }

    [Test]
    public void ParseWithOptions_ReturnsQuantityKindFailureWithoutThrowing()
    {
        IQuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();
        QuantityParseOptions options = QuantityParseOptions.CreateDefault().WithQuantityKind(new StandardQuantityKinds().Length());

        QuantityParseResult result = parser.ParseWithOptions("3 s", registry, options);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(result.GetPosition(), Is.EqualTo(2));
        Assert.That(result.GetMessage(), Does.Contain("length"));
    }

    [Test]
    public void ParseWithOptions_UsesQuantityKindAndUnitSystemToNarrowAmbiguousUnits()
    {
        IQuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateAmbiguousUnitRegistry();
        QuantityKindDefinition length = new QuantityKindDefinition("length", "Length", DimensionVector.Length(), "alpha", new string[0], new string[] { "beta" });
        UnitSystemDefinition system = new UnitSystemDefinition("beta_system", "Beta system", new string[0], new string[] { "length" }, new string[] { "beta" });
        QuantityParseOptions quantityKindOptions = QuantityParseOptions.CreateDefault().WithQuantityKind(new QuantityKindDefinition("alpha_length", "Alpha length", DimensionVector.Length(), "alpha", new string[0], new string[0]));
        QuantityParseOptions combinedOptions = QuantityParseOptions.CreateDefault().WithQuantityKind(length).WithUnitSystem(system);

        QuantityParseResult byQuantityKind = parser.ParseWithOptions("2 u", registry, quantityKindOptions);
        QuantityValue byCombinedContext = parser.ParseOrThrowWithOptions("3 u", registry, combinedOptions);

        Assert.That(byQuantityKind.IsSuccess(), Is.True);
        Assert.That(byQuantityKind.GetQuantity().GetUnit().GetId(), Is.EqualTo("alpha"));
        Assert.That(byCombinedContext.GetValue(), Is.EqualTo(3.0d));
        Assert.That(byCombinedContext.GetUnit().GetId(), Is.EqualTo("beta"));
    }

    [Test]
    public void ParseWithOptions_AppliesStrictWhitespaceToTheUnitExpression()
    {
        IQuantityParser parser = QuantityParser.CreateDefault();
        UnitRegistry registry = CreateRegistry();
        QuantityParseOptions strict = QuantityParseOptions.CreateDefault().WithStrict(true);

        QuantityParseResult compact = parser.ParseWithOptions("1 m/s", registry, strict);
        QuantityParseResult spacedUnit = parser.ParseWithOptions("1 m / s", registry, strict);
        QuantityParseResult relaxed = parser.ParseWithOptions(" 1 m / s ", registry, strict.WithStrict(false));

        Assert.That(compact.IsSuccess(), Is.True);
        Assert.That(spacedUnit.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(spacedUnit.GetPosition(), Is.EqualTo(3));
        Assert.That(relaxed.IsSuccess(), Is.True);
    }

    static UnitRegistry CreateRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        new StandardUnits().RegisterSiBaseUnits(registry);
        new StandardUnits().RegisterSiDerivedUnits(registry);
        new StandardUnits().RegisterMetricUnits(registry);
        new StandardUnits().RegisterImperialAndUsCustomaryUnits(registry);
        new StandardUnits().RegisterDataUnits(registry);
        new StandardPrefixes().Register(registry);
        return registry;
    }

    static UnitRegistry CreateAmbiguousUnitRegistry()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition alpha = new UnitDefinition("alpha", "alpha", "alphas", "u", new string[0], new StandardDimensions().Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
        UnitDefinition beta = new UnitDefinition("beta", "beta", "betas", "u", new string[0], new StandardDimensions().Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);

        registry.AddUnit(alpha);
        registry.AddUnit(beta);
        return registry;
    }
}
