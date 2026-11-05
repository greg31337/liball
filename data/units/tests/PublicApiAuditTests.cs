using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class PublicApiAuditTests
{
    [Test]
    public void LookupResultFactory_CreatesEveryOutcomeAndIsolatesUnitArrays()
    {
        LookupResultFactory factory = new LookupResultFactory();
        UnitDefinition meter = new StandardUnits().Meter();
        UnitDefinition foot = new StandardUnits().Foot();
        UnitDefinition[] source = new UnitDefinition[] { meter };

        UnitLookupResult success = factory.CreateUnitResult(source, "meter");
        source[0] = foot;
        UnitLookupResult missing = factory.CreateUnitResult(new UnitDefinition[0], "missing");
        UnitLookupResult ambiguous = factory.CreateUnitResult(new UnitDefinition[] { meter, foot }, "length");
        UnitDefinition[] returned = ambiguous.GetUnits();
        returned[0] = foot;

        Assert.That(success.IsSuccess(), Is.True);
        Assert.That(success.GetUnit(), Is.SameAs(meter));
        Assert.That(missing.IsFailure(), Is.True);
        Assert.That(missing.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.That(missing.GetMessage(), Does.Contain("missing"));
        Assert.That(ambiguous.IsAmbiguous(), Is.True);
        Assert.That(ambiguous.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupAmbiguous));
        Assert.That(ambiguous.GetUnits(), Is.EqualTo(new UnitDefinition[] { meter, foot }));
    }

    [Test]
    public void LookupResultFactory_RejectsNullArraysEntriesAndEmptyLookupText()
    {
        LookupResultFactory factory = new LookupResultFactory();

        Assert.Throws<System.ArgumentNullException>(() => factory.CreateUnitResult(null!, "unit"));
        Assert.Throws<System.ArgumentNullException>(() => factory.CreateQuantityKindResult(null!, "kind"));
        Assert.Throws<System.ArgumentNullException>(() => factory.CreateUnitSystemResult(null!, "system"));
        Assert.Throws<System.ArgumentNullException>(() => factory.CreatePhysicalConstantResult(null!, "constant"));
        Assert.Throws<System.ArgumentNullException>(() => factory.CreateUnitResult(new UnitDefinition[] { null! }, "unit"));
        Assert.Throws<System.ArgumentNullException>(() => factory.CreateQuantityKindResult(new QuantityKindDefinition[] { null! }, "kind"));
        Assert.Throws<System.ArgumentNullException>(() => factory.CreateUnitSystemResult(new UnitSystemDefinition[] { null! }, "system"));
        Assert.Throws<System.ArgumentNullException>(() => factory.CreatePhysicalConstantResult(new PhysicalConstantDefinition[] { null! }, "constant"));
        Assert.Throws<System.ArgumentException>(() => factory.CreateUnitResult(new UnitDefinition[0], null!));
        Assert.Throws<System.ArgumentException>(() => factory.CreateQuantityKindResult(new QuantityKindDefinition[0], ""));
        Assert.Throws<System.ArgumentException>(() => factory.CreateUnitSystemResult(new UnitSystemDefinition[0], " "));
        Assert.Throws<System.ArgumentException>(() => factory.CreatePhysicalConstantResult(new PhysicalConstantDefinition[0], "\t"));
    }

    [Test]
    public void EquivalencyConverter_CanConvertReportsCompatibilityAndNulls()
    {
        EquivalencyConverter converter = new EquivalencyConverter();
        EquivalencyDefinition equivalency = new StandardEquivalencies().WavelengthFrequency();
        UnitDefinition meter = new StandardUnits().Meter();
        UnitDefinition hertz = new StandardUnits().Hertz();
        UnitDefinition affineLength = AffineLength();

        Assert.That(converter.CanConvert(meter, hertz, equivalency), Is.True);
        Assert.That(converter.CanConvert(hertz, meter, equivalency), Is.True);
        Assert.That(converter.CanConvert(new StandardUnits().Kilogram(), hertz, equivalency), Is.False);
        Assert.That(converter.CanConvert(affineLength, hertz, equivalency), Is.False);
        Assert.That(converter.CanConvert(meter, affineLength, equivalency), Is.False);
        Assert.That(converter.CanConvert(null, hertz, equivalency), Is.False);
        Assert.That(converter.CanConvert(meter, null, equivalency), Is.False);
        Assert.That(converter.CanConvert(meter, hertz, null), Is.False);
    }

    [Test]
    public void EquivalencyConverter_DirectMethodsRejectEveryMissingInput()
    {
        EquivalencyConverter converter = new EquivalencyConverter();
        UnitDefinition meter = new StandardUnits().Meter();
        UnitDefinition hertz = new StandardUnits().Hertz();
        EquivalencyDefinition equivalency = new StandardEquivalencies().WavelengthFrequency();
        QuantityValue quantity = new QuantityValue(1.0d, meter);
        UnitRegistry registry = new StandardRegistry().Create();

        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertValue(1.0d, null!, hertz, equivalency));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertValue(1.0d, meter, null!, equivalency));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertValue(1.0d, meter, hertz, null!));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertById(null!, hertz, registry, "wavelength_frequency"));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertById(quantity, null!, registry, "wavelength_frequency"));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertById(quantity, hertz, null!, "wavelength_frequency"));
        Assert.Throws<System.ArgumentException>(() => converter.ConvertById(quantity, hertz, registry, null!));
        Assert.Throws<System.ArgumentException>(() => converter.ConvertById(quantity, hertz, registry, " "));
    }

    [Test]
    public void EquivalencyConverter_ReportsScaleOnlyAndReverseReciprocalFailures()
    {
        EquivalencyConverter converter = new EquivalencyConverter();
        EquivalencyDefinition equivalency = new StandardEquivalencies().WavelengthFrequency();

        ConversionResult affine = converter.ConvertValue(1.0d, AffineLength(), new StandardUnits().Hertz(), equivalency);
        ConversionResult reverseZero = converter.ConvertValue(0.0d, new StandardUnits().Hertz(), new StandardUnits().Meter(), equivalency);

        Assert.That(affine.IsFailure(), Is.True);
        Assert.That(affine.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
        Assert.That(reverseZero.IsFailure(), Is.True);
        Assert.That(reverseZero.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
    }

    [Test]
    public void PhysicalConstantLookupResult_ExposesStatusMessagesAndIsolatedCandidates()
    {
        LookupResultFactory factory = new LookupResultFactory();
        PhysicalConstantDefinition first = Constant("first");
        PhysicalConstantDefinition second = Constant("second");
        PhysicalConstantDefinition[] source = new PhysicalConstantDefinition[] { first };

        PhysicalConstantLookupResult success = factory.CreatePhysicalConstantResult(source, "first");
        source[0] = second;
        PhysicalConstantLookupResult missing = factory.CreatePhysicalConstantResult(new PhysicalConstantDefinition[0], "missing");
        PhysicalConstantLookupResult ambiguous = factory.CreatePhysicalConstantResult(new PhysicalConstantDefinition[] { first, second }, "shared");
        PhysicalConstantDefinition[] returned = ambiguous.GetPhysicalConstants();
        returned[0] = second;

        Assert.That(success.IsSuccess(), Is.True);
        Assert.That(success.IsFailure(), Is.False);
        Assert.That(success.GetMessage(), Is.Empty);
        Assert.That(success.GetPhysicalConstant(), Is.SameAs(first));
        Assert.That(missing.IsSuccess(), Is.False);
        Assert.That(missing.GetMessage(), Does.Contain("missing"));
        Assert.That(ambiguous.IsAmbiguous(), Is.True);
        Assert.That(ambiguous.GetPhysicalConstants(), Is.EqualTo(new PhysicalConstantDefinition[] { first, second }));
    }

    [Test]
    public void OptionalConstructorMetadata_NormalizesNullsToEmptyValues()
    {
        EquivalencyDefinition equivalency = new EquivalencyDefinition(
            "length_time",
            "Length-time",
            null,
            "length",
            new StandardDimensions().Length(),
            "time",
            new StandardDimensions().Time(),
            new RationalNumber(1, 1),
            1,
            new RationalNumber(1, 1),
            1,
            null);
        PhysicalConstantDefinition constant = new PhysicalConstantDefinition(
            "reference",
            "Reference",
            null,
            null,
            "exact",
            "test source",
            "test edition",
            0.0d,
            new QuantityValue(1.0d, new StandardUnits().Meter()),
            null);

        Assert.That(equivalency.GetDescription(), Is.Empty);
        Assert.That(equivalency.GetAliases(), Is.Empty);
        Assert.That(constant.GetSymbol(), Is.Empty);
        Assert.That(constant.GetDescription(), Is.Empty);
        Assert.That(constant.GetAliases(), Is.Empty);
    }

    [Test]
    public void RemainingNamedAndToUnitMethodsReturnExpectedValues()
    {
        Pressure pressure = Pressure.FromPascals(6894.757293168361d);
        Power power = Power.FromWatts(745.6998715822702d);
        TemperatureInterval interval = TemperatureInterval.FromFahrenheit(18.0d).ToUnit(new StandardUnits().CelsiusInterval());

        Assert.That(pressure.Psi(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(power.Horsepower(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(interval.GetUnit().GetId(), Is.EqualTo("celsius_interval"));
        Assert.That(interval.GetValue(), Is.EqualTo(10.0d).Within(0.000000000001d));
    }

    [Test]
    public void UnitParserEntryPointsHandleMissingCatalogsAndOptionsConsistently()
    {
        UnitParser parser = new UnitParser();
        UnitRegistry registry = new StandardRegistry().Create();

        UnitParseResult missingCatalog = parser.ParseWithOptions("meter", null!, UnitParseOptions.CreateDefault());
        UnitParseResult missingOptions = parser.ParseWithOptions("meter", registry, null!);

        Assert.That(missingCatalog.IsFailure(), Is.True);
        Assert.That(missingCatalog.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(missingOptions.IsFailure(), Is.True);
        Assert.That(missingOptions.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.Throws<System.ArgumentNullException>(() => parser.ParseOrThrow("meter", null!));
        Assert.Throws<System.ArgumentNullException>(() => parser.ParseOrThrowWithOptions("meter", registry, null!));
    }

    [Test]
    public void UnitSystemPreferenceProbeHandlesNullAndEmptyKindIds()
    {
        UnitSystemDefinition system = new StandardUnitSystems().Si();

        Assert.That(system.HasPreferredUnitFor(null), Is.False);
        Assert.That(system.HasPreferredUnitFor(""), Is.False);
        Assert.That(system.HasPreferredUnitFor(" "), Is.False);
    }

    [Test]
    public void QuantityKindValidation_PreservesTemperatureSemanticsAcrossPublicConstructionPaths()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityKindDefinition temperature = new StandardQuantityKinds().Temperature();
        QuantityParseOptions parseOptions = QuantityParseOptions.CreateDefault().WithQuantityKind(temperature);
        QuantityFactory factory = new QuantityFactory(new UnitParser());

        QuantityParseResult parsed = QuantityParser.CreateDefault().ParseWithOptions("1 celsius_interval", registry, parseOptions);
        QuantityConstructionResult byId = factory.CreateFromUnitId(1.0d, "celsius_interval", registry, temperature);
        QuantityConstructionResult bySymbol = factory.CreateFromUnitSymbol(1.0d, "delta_degC", registry, temperature);
        QuantityConstructionResult byExpression = factory.CreateFromUnitExpression(1.0d, "celsius_interval", registry, temperature);
        QuantityConstructionResult compatibleDynamicScale = factory.CreateFromUnitExpression(
            1.0d,
            "km",
            registry,
            new StandardQuantityKinds().Length());

        Assert.That(parsed.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(byId.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(bySymbol.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(byExpression.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(compatibleDynamicScale.IsSuccess(), Is.True, compatibleDynamicScale.GetMessage());
        Assert.That(compatibleDynamicScale.GetQuantity().GetUnit().GetId(), Is.EqualTo("kilometer"));
    }

    [Test]
    public void UnitParserContext_DoesNotNarrowAffineKindsToScaleOnlySameDimensionUnits()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition kelvin = new StandardUnits().Kelvin();
        UnitDefinition interval = new UnitDefinition(
            "custom_interval",
            "custom interval",
            "custom intervals",
            "u",
            new string[0],
            new StandardDimensions().ThermodynamicTemperature(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
        UnitDefinition length = new UnitDefinition(
            "custom_length",
            "custom length",
            "custom lengths",
            "u",
            new string[0],
            new StandardDimensions().Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
        registry.AddUnit(kelvin);
        registry._units.Add(interval);
        registry._units.Add(length);
        QuantityKindDefinition temperature = new StandardQuantityKinds().Temperature();

        UnitParseResult result = new UnitParser().ParseWithOptions(
            "u",
            registry,
            UnitParseOptions.CreateDefault().WithQuantityKind(temperature));

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseUnknownUnit));
    }

    [Test]
    public void UnitParserContext_CanSelectAUnitSystemPreferredUnitWithoutQuantityKindContext()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition alpha = CustomLength("alpha", "u");
        UnitDefinition beta = CustomLength("beta", "u");
        registry.AddUnit(alpha);
        registry.AddUnit(beta);
        UnitSystemDefinition system = new UnitSystemDefinition(
            "preferred_beta",
            "Preferred beta",
            new string[0],
            new string[] { "length" },
            new string[] { "beta" });

        UnitParseResult result = new UnitParser().ParseWithOptions(
            "u",
            registry,
            UnitParseOptions.CreateDefault().WithUnitSystem(system));

        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetUnit(), Is.SameAs(beta));
    }

    static UnitDefinition AffineLength()
    {
        return new UnitDefinition(
            "affine_length",
            "affine length",
            "affine lengths",
            "al",
            new string[0],
            new StandardDimensions().Length(),
            new RationalNumber(1, 1),
            new RationalNumber(1, 1),
            false,
            UnitConversionKind.Affine);
    }

    static UnitDefinition CustomLength(string id, string symbol)
    {
        return new UnitDefinition(
            id,
            id,
            id,
            symbol,
            new string[0],
            new StandardDimensions().Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }

    static PhysicalConstantDefinition Constant(string id)
    {
        return new PhysicalConstantDefinition(
            id,
            id,
            id,
            "",
            "exact",
            "test source",
            "test edition",
            0.0d,
            new QuantityValue(1.0d, new StandardUnits().Meter()),
            new string[0]);
    }
}
