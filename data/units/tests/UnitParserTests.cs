using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitParserTests
{
    [Test]
    public void ParseOrThrow_ParsesUnitSymbolsAndAliases()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition meter = parser.ParseOrThrow("m", registry);
        UnitDefinition liter = parser.ParseOrThrow("litre", registry);

        Assert.That(meter.GetId(), Is.EqualTo("meter"));
        Assert.That(liter.GetId(), Is.EqualTo("liter"));
    }

    [Test]
    public void ParseOrThrow_ParsesSimpleUnitNamesPluralsAndWhitespace()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition meter = parser.ParseOrThrow(" meter ", registry);
        UnitDefinition meters = parser.ParseOrThrow("meters", registry);
        UnitDefinition seconds = parser.ParseOrThrow("seconds", registry);

        Assert.That(meter.GetId(), Is.EqualTo("meter"));
        Assert.That(meters.GetId(), Is.EqualTo("meter"));
        Assert.That(seconds.GetId(), Is.EqualTo("second"));
    }

    [Test]
    public void ParseOrThrow_ParsesMultiplicationAndDivision()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition energy = parser.ParseOrThrow("N*m", registry);
        UnitDefinition speed = parser.ParseOrThrow("m/s", registry);

        Assert.That(energy.GetId(), Is.EqualTo("(newton*meter)"));
        Assert.That(energy.GetDimension().EqualsDimension(new StandardUnits().Joule().GetDimension()), Is.True);
        Assert.That(speed.GetId(), Is.EqualTo("(meter/second)"));
        Assert.That(speed.GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time())), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesCompoundExpressionsWithWhitespace()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition force = parser.ParseOrThrow(" kg * m / s2 ", registry);

        Assert.That(force.GetId(), Is.EqualTo("((kilogram*meter)/(second^2))"));
        Assert.That(force.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
        Assert.That(force.GetDimension().EqualsDimension(new StandardUnits().Newton().GetDimension()), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesCaretAndCompactIntegerPowers()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition acceleration = parser.ParseOrThrow("m/s^2", registry);
        UnitDefinition area = parser.ParseOrThrow("m2", registry);

        Assert.That(acceleration.GetId(), Is.EqualTo("(meter/(second^2))"));
        Assert.That(acceleration.GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2))), Is.True);
        Assert.That(area.GetId(), Is.EqualTo("(meter^2)"));
        Assert.That(area.GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2)), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesMetricPrefixedUnitSymbolsAndNames()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition kilometer = parser.ParseOrThrow("km", registry);
        UnitDefinition kilometerName = parser.ParseOrThrow("kilometer", registry);
        UnitDefinition millisecond = parser.ParseOrThrow("ms", registry);

        Assert.That(kilometer.GetId(), Is.EqualTo("kilometer"));
        Assert.That(kilometer.GetName(), Is.EqualTo("kilometer"));
        Assert.That(kilometer.GetSymbol(), Is.EqualTo("km"));
        Assert.That(kilometer.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1000, 1)), Is.True);
        Assert.That(kilometerName.GetId(), Is.EqualTo("kilometer"));
        Assert.That(millisecond.GetId(), Is.EqualTo("milli_second"));
        Assert.That(millisecond.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1000)), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesMetricPrefixedAliasesAndMicroSymbol()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition kilometre = parser.ParseOrThrow("kilometre", registry);
        UnitDefinition micrometer = parser.ParseOrThrow("\u03bcm", registry);

        Assert.That(kilometre.GetId(), Is.EqualTo("kilometer"));
        Assert.That(kilometre.GetSymbol(), Is.EqualTo("km"));
        Assert.That(micrometer.GetId(), Is.EqualTo("micro_meter"));
        Assert.That(micrometer.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1000000)), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesMetricPrefixedUnitsInsideCompoundExpressions()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition speed = parser.ParseOrThrow("km/s", registry);
        UnitDefinition area = parser.ParseOrThrow("mm2", registry);

        Assert.That(speed.GetId(), Is.EqualTo("(kilometer/second)"));
        Assert.That(speed.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1000, 1)), Is.True);
        Assert.That(speed.GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time())), Is.True);
        Assert.That(area.GetId(), Is.EqualTo("(milli_meter^2)"));
        Assert.That(area.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1000000)), Is.True);
        Assert.That(area.GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2)), Is.True);
    }

    [Test]
    public void ParseOrThrow_PrefersRegisteredUnitBeforePrefixParsing()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition kilogram = parser.ParseOrThrow("kg", registry);
        UnitDefinition kilobyte = parser.ParseOrThrow("kB", registry);

        Assert.That(kilogram.GetId(), Is.EqualTo("kilogram"));
        Assert.That(kilogram.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
        Assert.That(kilobyte.GetId(), Is.EqualTo("kilobyte"));
        Assert.That(kilobyte.GetScaleToSystemUnit().EqualsValue(new RationalNumber(8000, 1)), Is.True);
    }

    [Test]
    public void ParseOrThrow_RejectsMetricPrefixForNonPrefixableUnits()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("kday", registry));
        Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("kkg", registry));
    }

    [Test]
    public void ParseOrThrow_RejectsAmbiguousMetricPrefixParsing()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition alpha = new UnitDefinition("alpha", "alpha", "alphas", "u", new string[0], new StandardDimensions().Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
        UnitDefinition beta = new UnitDefinition("beta", "beta", "betas", "u", new string[0], new StandardDimensions().Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);

        registry.AddPrefix(new PrefixDefinition("kilo", "kilo", "k", new RationalNumber(1000, 1)));
        registry.AddUnit(alpha);
        registry.AddUnit(beta);

        Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("ku", registry));
    }

    [Test]
    public void ParseOrThrow_RegisteredSymbolPreventsAmbiguousPrefixSplit()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition alpha = new UnitDefinition("alpha", "alpha", "alphas", "a", new string[0], new StandardDimensions().Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
        UnitDefinition exact = new UnitDefinition("exact_xa", "exact xa", "exact xa", "xa", new string[0], new StandardDimensions().Length(), new RationalNumber(7, 1), new RationalNumber(0, 1), false, UnitConversionKind.Scale);

        registry.AddPrefix(new PrefixDefinition("x_prefix", "xeno", "x", new RationalNumber(10, 1)));
        registry.AddUnit(alpha);
        registry.AddUnit(exact);

        UnitDefinition parsed = parser.ParseOrThrow("xa", registry);

        Assert.That(parsed.GetId(), Is.EqualTo("exact_xa"));
        Assert.That(parsed.GetScaleToSystemUnit().EqualsValue(new RationalNumber(7, 1)), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesSignedIntegerPowers()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition frequency = parser.ParseOrThrow("s^-1", registry);

        Assert.That(frequency.GetId(), Is.EqualTo("(second^-1)"));
        Assert.That(frequency.GetDimension().EqualsDimension(new StandardDimensions().Time().Pow(-1)), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesParenthesizedExpressions()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition squaredSpeed = parser.ParseOrThrow("(m/s)^2", registry);

        Assert.That(squaredSpeed.GetId(), Is.EqualTo("((meter/second)^2)"));
        Assert.That(squaredSpeed.GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2).Divide(new StandardDimensions().Time().Pow(2))), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesNestedParenthesizedExpressions()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition result = parser.ParseOrThrow("(N*m)/(s)", registry);

        Assert.That(result.GetId(), Is.EqualTo("((newton*meter)/second)"));
        Assert.That(result.GetDimension().EqualsDimension(new StandardUnits().Joule().GetDimension().Divide(new StandardDimensions().Time())), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesNestedPrefixedExpressionWithSignedPowers()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition result = parser.ParseOrThrow("((km*kg)/(ms^2))*s^-1", registry);

        Assert.That(result.GetId(), Is.EqualTo("(((kilometer*kilogram)/(milli_second^2))*(second^-1))"));
        Assert.That(result.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1000000000, 1)), Is.True);
        Assert.That(result.GetDimension().EqualsDimension(new StandardDimensions().Mass().Multiply(new StandardDimensions().Length()).Divide(new StandardDimensions().Time().Pow(3))), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesDimensionlessRatioOfCompoundDataRates()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition result = parser.ParseOrThrow("(kB/s)/(MB/s)", registry);

        Assert.That(result.GetId(), Is.EqualTo("((kilobyte/second)/(megabyte/second))"));
        Assert.That(result.GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1000)), Is.True);
        Assert.That(result.GetDimension().IsDimensionless(), Is.True);
    }

    [Test]
    public void ParseOrThrow_ParsesDimensionlessOneInCompoundExpressions()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition dimensionless = parser.ParseOrThrow("1", registry);
        UnitDefinition inverseSecond = parser.ParseOrThrow("1/s", registry);

        Assert.That(dimensionless.GetId(), Is.EqualTo("one"));
        Assert.That(dimensionless.GetDimension().IsDimensionless(), Is.True);
        Assert.That(inverseSecond.GetId(), Is.EqualTo("(one/second)"));
        Assert.That(inverseSecond.GetDimension().EqualsDimension(new StandardDimensions().Time().Pow(-1)), Is.True);
    }

    [Test]
    public void ParseOrThrow_RejectsUnknownAndAmbiguousUnits()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("unknown", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("m$", registry));
    }

    [Test]
    public void ParseOrThrow_RejectsExactAmbiguousUnitSymbols()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition alpha = new UnitDefinition("alpha", "alpha", "alphas", "u", new string[0], new StandardDimensions().Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
        UnitDefinition beta = new UnitDefinition("beta", "beta", "betas", "u", new string[0], new StandardDimensions().Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);

        registry.AddUnit(alpha);
        registry.AddUnit(beta);

        System.InvalidOperationException exception = Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("u", registry))!;

        Assert.That(exception.Message, Does.Contain("ambiguous"));
    }

    [Test]
    public void ParseOrThrow_ReportsUnknownUnitText()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        System.InvalidOperationException exception = Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("zzunit", registry))!;

        Assert.That(exception.Message, Is.EqualTo("Unit was not found: zzunit"));
    }

    [Test]
    public void ParseOrThrow_RejectsMalformedNestedCompoundExpressions()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("((km*kg)/(ms^2))*", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("((km*kg)/(ms^2))*s^-1)", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("(kB/s)/(MB/)", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("m^^2", registry));
    }

    [Test]
    public void ParseOrThrow_RejectsInvalidUnitsInsideCompoundExpressions()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        System.InvalidOperationException nonPrefixable = Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("(km*kday)/s", registry))!;
        System.InvalidOperationException affineProduct = Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("K*m", registry))!;
        System.InvalidOperationException affinePower = Assert.Throws<System.InvalidOperationException>(() => parser.ParseOrThrow("(K)^2", registry))!;

        Assert.That(nonPrefixable.Message, Does.Contain("kday"));
        Assert.That(affineProduct.Message, Does.Contain("scale-only"));
        Assert.That(affinePower.Message, Does.Contain("scale-only"));
    }

    [Test]
    public void ParseOrThrow_RejectsInvalidSyntax()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("m/", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("(m/s", registry));
        Assert.Throws<System.ArgumentException>(() => parser.ParseOrThrow("m^", registry));
    }

    [Test]
    public void ParseOrThrow_RejectsMissingInputs()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        Assert.Throws<System.ArgumentNullException>(() => parser.ParseOrThrow(null!, registry));
        Assert.Throws<System.ArgumentNullException>(() => parser.ParseOrThrow("m", null!));
    }

    [Test]
    public void Parse_ReturnsSuccessfulPublicResultWithoutThrowing()
    {
        IUnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitParseResult result = parser.Parse("km/s", registry);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetUnit().GetId(), Is.EqualTo("(kilometer/second)"));
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(result.GetMessage(), Is.EqualTo(""));
        Assert.That(result.GetPosition(), Is.EqualTo(-1));
        Assert.That(result.GetCandidates(), Is.Empty);
    }

    [Test]
    public void Parse_ReturnsStableSyntaxAndUnknownUnitFailures()
    {
        IUnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitParseResult malformed = parser.Parse("m/", registry);
        UnitParseResult unknown = parser.Parse("m/zzunit", registry);
        UnitParseResult exponentOverflow = parser.Parse("m^2147483648", registry);
        UnitParseResult minimumExponent = parser.Parse("m^-2147483648", registry);
        UnitParseResult missingText = parser.Parse(null!, registry);

        Assert.That(malformed.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(malformed.GetPosition(), Is.EqualTo(2));
        Assert.That(malformed.GetMessage(), Does.Contain("position 2"));
        Assert.That(unknown.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseUnknownUnit));
        Assert.That(unknown.GetPosition(), Is.EqualTo(2));
        Assert.That(unknown.GetMessage(), Does.Contain("zzunit"));
        Assert.That(unknown.GetCandidates(), Is.Empty);
        Assert.That(exponentOverflow.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(exponentOverflow.GetPosition(), Is.EqualTo(2));
        Assert.That(minimumExponent.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
        Assert.That(minimumExponent.GetPosition(), Is.EqualTo(1));
        Assert.That(missingText.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(missingText.GetPosition(), Is.EqualTo(0));
    }

    [Test]
    public void Parse_ReturnsAmbiguityCandidatesAtTheirSourcePosition()
    {
        IUnitParser parser = new UnitParser();
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition alpha = new UnitDefinition("alpha", "alpha", "alphas", "u", new string[0], new StandardDimensions().Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
        UnitDefinition beta = new UnitDefinition("beta", "beta", "betas", "u", new string[0], new StandardDimensions().Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
        registry.AddUnit(alpha);
        registry.AddUnit(beta);

        UnitParseResult result = parser.Parse("1/u", registry);
        UnitDefinition[] candidates = result.GetCandidates();
        candidates[0] = beta;

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseAmbiguousUnit));
        Assert.That(result.GetPosition(), Is.EqualTo(2));
        Assert.That(result.GetCandidates(), Is.EqualTo(new UnitDefinition[] { alpha, beta }));
        Assert.Throws<System.InvalidOperationException>(() => result.GetUnit());
    }

    [Test]
    public void ParseWithOptions_QuantityKindNarrowsAmbiguousUnits()
    {
        IUnitParser parser = new UnitParser();
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition alpha = ContextUnit("alpha", "u");
        UnitDefinition beta = ContextUnit("beta", "u");
        registry.AddUnit(alpha);
        registry.AddUnit(beta);
        QuantityKindDefinition alphaKind = new QuantityKindDefinition("alpha_length", "Alpha length", DimensionVector.Length(), "alpha", new string[0], new string[0]);
        QuantityKindDefinition time = new QuantityKindDefinition("time", "Time", DimensionVector.Time(), "second", new string[0], new string[0]);

        UnitParseResult narrowed = parser.ParseWithOptions("u", registry, UnitParseOptions.CreateDefault().WithQuantityKind(alphaKind));
        UnitParseResult excluded = parser.ParseWithOptions("u", registry, UnitParseOptions.CreateDefault().WithQuantityKind(time));

        Assert.That(narrowed.IsSuccess(), Is.True);
        Assert.That(narrowed.GetUnit(), Is.SameAs(alpha));
        Assert.That(excluded.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseUnknownUnit));
        Assert.That(excluded.GetCandidates(), Is.Empty);
    }

    [Test]
    public void ParseWithOptions_UnitSystemAndCombinedContextNarrowAmbiguousUnits()
    {
        IUnitParser parser = new UnitParser();
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition alpha = ContextUnit("alpha", "u");
        UnitDefinition beta = ContextUnit("beta", "u");
        registry.AddUnit(alpha);
        registry.AddUnit(beta);
        QuantityKindDefinition length = new QuantityKindDefinition("length", "Length", DimensionVector.Length(), "alpha", new string[0], new string[] { "beta" });
        UnitSystemDefinition betaBaseSystem = new UnitSystemDefinition("beta_base", "Beta base", new string[] { "beta" }, new string[0], new string[0]);
        UnitSystemDefinition betaPreferredSystem = new UnitSystemDefinition("beta_preferred", "Beta preferred", new string[0], new string[] { "length" }, new string[] { "beta" });

        UnitDefinition bySystem = parser.ParseOrThrowWithOptions("u", registry, UnitParseOptions.CreateDefault().WithUnitSystem(betaBaseSystem));
        UnitDefinition byCombinedContext = parser.ParseOrThrowWithOptions("u", registry, UnitParseOptions.CreateDefault().WithQuantityKind(length).WithUnitSystem(betaPreferredSystem));

        Assert.That(bySystem, Is.SameAs(beta));
        Assert.That(byCombinedContext, Is.SameAs(beta));
    }

    [Test]
    public void ParseWithOptions_QuantityKindNarrowsAmbiguousPrefixedUnits()
    {
        IUnitParser parser = new UnitParser();
        UnitRegistry registry = new UnitRegistry();
        registry.AddPrefix(new PrefixDefinition("kilo", "kilo", "k", new RationalNumber(1000, 1)));
        registry.AddUnit(ContextUnit("alpha", "u"));
        registry.AddUnit(ContextUnit("beta", "u"));
        QuantityKindDefinition quantityKind = new QuantityKindDefinition("scaled_length", "Scaled length", DimensionVector.Length(), "kilo_alpha", new string[0], new string[0]);

        UnitParseResult result = parser.ParseWithOptions("ku", registry, UnitParseOptions.CreateDefault().WithQuantityKind(quantityKind));

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetUnit().GetId(), Is.EqualTo("kilo_alpha"));
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1000, 1)), Is.True);
    }

    [Test]
    public void ParseWithOptions_EnforcesStrictUnitWhitespacePolicy()
    {
        IUnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();
        UnitParseOptions strict = UnitParseOptions.CreateDefault().WithStrict(true);

        UnitParseResult compact = parser.ParseWithOptions("m/s", registry, strict);
        UnitParseResult spaced = parser.ParseWithOptions("m / s", registry, strict);
        UnitParseResult relaxed = parser.ParseWithOptions(" m / s ", registry, strict.WithStrict(false));

        Assert.That(compact.IsSuccess(), Is.True);
        Assert.That(spaced.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(spaced.GetPosition(), Is.EqualTo(1));
        Assert.That(relaxed.IsSuccess(), Is.True);
        Assert.Throws<System.ArgumentNullException>(() => parser.ParseOrThrowWithOptions("m", registry, null!));
    }

    [Test]
    public void ParseOrThrow_HandlesAliasesSignedSuffixPowersAndCanonicalText()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = CreateRegistry();

        UnitDefinition multiwordAlias = parser.ParseOrThrow("metric ton", registry);
        UnitDefinition hyphenatedName = parser.ParseOrThrow("pound-force/s", registry);
        UnitDefinition positivePower = parser.ParseOrThrow("m^+2", registry);
        UnitDefinition suffixPower = parser.ParseOrThrow("s-1", registry);
        UnitDefinition source = parser.ParseOrThrow("m/s^2", registry);
        string canonical = new UnitFormatter().FormatWithOptions(source, UnitFormatOptions.CreateCanonical());
        UnitDefinition canonicalRoundTrip = parser.ParseOrThrow(canonical, registry);

        Assert.That(multiwordAlias.GetId(), Is.EqualTo("tonne"));
        Assert.That(hyphenatedName.GetId(), Is.EqualTo("(pound_force/second)"));
        Assert.That(positivePower.GetId(), Is.EqualTo("(meter^2)"));
        Assert.That(suffixPower.GetId(), Is.EqualTo("(second^-1)"));
        Assert.That(canonicalRoundTrip.GetDimension().EqualsDimension(source.GetDimension()), Is.True);
        Assert.That(canonicalRoundTrip.GetScaleToSystemUnit().EqualsValue(source.GetScaleToSystemUnit()), Is.True);
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

    static UnitDefinition ContextUnit(string id, string symbol)
    {
        return new UnitDefinition(id, id, id, symbol, new string[0], DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitConversionKind.Scale);
    }
}
