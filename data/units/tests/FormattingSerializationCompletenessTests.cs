using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class FormattingSerializationCompletenessTests
{
    [Test]
    public void UnitFormatter_FormatsCustomMetadataWithEveryStyle()
    {
        UnitDefinition custom = CustomLengthUnit("widget/2", "widget two", "widget twos", "w2", new RationalNumber(3, 1), true);
        UnitFormatter formatter = new UnitFormatter();

        Assert.That(formatter.FormatWithOptions(custom, new UnitFormatOptions(UnitFormatStyle.Symbol)), Is.EqualTo("w2"));
        Assert.That(formatter.FormatWithOptions(custom, new UnitFormatOptions(UnitFormatStyle.Name)), Is.EqualTo("widget two"));
        Assert.That(
            formatter.FormatWithOptions(custom, new UnitFormatOptions(UnitFormatStyle.Canonical)),
            Is.EqualTo("[007700690064006700650074002F0032]"));
        Assert.That(formatter.FormatWithOptions(custom, new UnitFormatOptions(UnitFormatStyle.Decomposed)), Is.EqualTo("m"));
    }

    [Test]
    public void QuantityFormatter_AppliesNumericCultureUnitAndPluralizationOptions()
    {
        QuantityFormatter formatter = new QuantityFormatter();
        UnitDefinition meter = new StandardUnits().Meter();
        QuantityFormatOptions germanSymbols = new QuantityFormatOptions(
            "N2",
            System.Globalization.CultureInfo.GetCultureInfo("de-DE"),
            new UnitFormatOptions(UnitFormatStyle.Symbol));
        QuantityFormatOptions invariantNames = new QuantityFormatOptions(
            "G",
            System.Globalization.CultureInfo.InvariantCulture,
            new UnitFormatOptions(UnitFormatStyle.Name));
        QuantityFormatOptions canonical = invariantNames.WithUnitFormatOptions(new UnitFormatOptions(UnitFormatStyle.Canonical));
        QuantityFormatOptions decomposed = invariantNames.WithUnitFormatOptions(new UnitFormatOptions(UnitFormatStyle.Decomposed));

        Assert.That(formatter.FormatWithOptions(new QuantityValue(1234.5d, meter), germanSymbols), Is.EqualTo("1.234,50 m"));
        Assert.That(formatter.FormatWithOptions(new QuantityValue(1.0d, meter), invariantNames), Is.EqualTo("1 meter"));
        Assert.That(formatter.FormatWithOptions(new QuantityValue(-1.0d, meter), invariantNames), Is.EqualTo("-1 meter"));
        Assert.That(formatter.FormatWithOptions(new QuantityValue(0.0d, meter), invariantNames), Is.EqualTo("0 meters"));
        Assert.That(formatter.FormatWithOptions(new QuantityValue(1.5d, meter), invariantNames), Is.EqualTo("1.5 meters"));
        Assert.That(formatter.FormatWithOptions(new QuantityValue(double.NaN, meter), invariantNames), Is.EqualTo("NaN meters"));
        Assert.That(formatter.FormatWithOptions(new QuantityValue(double.PositiveInfinity, meter), invariantNames), Is.EqualTo("Infinity meters"));
        Assert.That(formatter.FormatWithOptions(new QuantityValue(2.0d, meter), canonical), Is.EqualTo("2 meter"));
        Assert.That(formatter.FormatWithOptions(new QuantityValue(2.0d, new StandardUnits().Newton()), decomposed), Is.EqualTo("2 m*kg/s^2"));
    }

    [Test]
    public void CanonicalSerialization_RoundTripsEveryRegisteredStandardUnit()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        UnitDefinition[] units = registry.GetUnits();
        QuantitySerializer serializer = new QuantitySerializer();

        Assert.That(units, Is.Not.Empty);
        int index = 0;
        while (index < units.Length)
        {
            double value = index + 0.125d;
            string text = serializer.SerializeCanonical(new QuantityValue(value, units[index]));
            QuantityParseResult result = serializer.DeserializeCanonicalResult(text, registry);

            Assert.That(result.IsSuccess(), Is.True, units[index].GetId() + ": " + result.GetMessage());
            Assert.That(result.GetQuantity().GetValue(), Is.EqualTo(value), units[index].GetId());
            Assert.That(result.GetQuantity().GetUnit().IsEquivalentTo(units[index]), Is.True, units[index].GetId());
            index = index + 1;
        }
    }

    [Test]
    public void CanonicalSerialization_RoundTripsPrefixedCustomCompoundUnitsDeterministically()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition custom = CustomLengthUnit("widget/2", "widget two", "widget twos", "w", new RationalNumber(3, 1), true);
        UnitDefinition second = new StandardUnits().Second();
        UnitDefinition kilogram = new StandardUnits().Kilogram();
        registry.AddUnit(custom);
        registry.AddUnit(second);
        registry.AddUnit(kilogram);
        registry.AddPrefix(new PrefixDefinition("double", "double", "d", new RationalNumber(2, 1)));
        UnitDefinition prefixed = new UnitParser().ParseOrThrow("dw", registry);
        UnitDefinition left = prefixed.Multiply(second).Divide(new StandardUnits().Kilogram());
        UnitDefinition right = prefixed.Multiply(second).Divide(new StandardUnits().Kilogram());
        QuantitySerializer serializer = new QuantitySerializer();

        string leftText = serializer.SerializeCanonical(new QuantityValue(-7.25d, left));
        string rightText = serializer.SerializeCanonical(new QuantityValue(-7.25d, right));
        QuantityParseResult result = serializer.DeserializeCanonicalResult(leftText, registry);

        Assert.That(leftText, Is.EqualTo(rightText));
        Assert.That(leftText, Does.StartWith("-7.25 (("));
        Assert.That(leftText, Does.Contain("second"));
        Assert.That(leftText, Does.Contain("kilogram"));
        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetQuantity().GetUnit().IsEquivalentTo(left), Is.True);
    }

    [Test]
    public void DtoQuantityKindValidation_AcceptsEveryDeclaredStandardMappingAndDynamicScaleUnit()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        QuantitySerializer serializer = new QuantitySerializer();
        QuantityKindDefinition[] quantityKinds = registry.GetQuantityKinds();

        int kindIndex = 0;
        while (kindIndex < quantityKinds.Length)
        {
            QuantityKindDefinition quantityKind = quantityKinds[kindIndex];
            string[] acceptedUnitIds = quantityKind.GetAcceptedUnitIds();
            int unitIndex = 0;
            while (unitIndex < acceptedUnitIds.Length)
            {
                QuantityConstructionResult result = serializer.FromDtoResult(
                    QuantityDto.Create(1.0d, acceptedUnitIds[unitIndex], quantityKind.GetId()),
                    registry);

                Assert.That(result.IsSuccess(), Is.True, quantityKind.GetId() + ":" + acceptedUnitIds[unitIndex] + ":" + result.GetMessage());
                unitIndex = unitIndex + 1;
            }

            kindIndex = kindIndex + 1;
        }

        QuantityConstructionResult prefixed = serializer.FromDtoResult(QuantityDto.Create(2.0d, "kilometer", "length"), registry);
        Assert.That(prefixed.IsSuccess(), Is.True, prefixed.GetMessage());
        Assert.That(prefixed.GetQuantity().GetUnit().GetId(), Is.EqualTo("kilometer"));
    }

    [Test]
    public void DtoQuantityKindValidation_PreservesSameDimensionTemperatureSemantics()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        QuantitySerializer serializer = new QuantitySerializer();

        QuantityConstructionResult point = serializer.FromDtoResult(QuantityDto.Create(20.0d, "degree_celsius", "temperature"), registry);
        QuantityConstructionResult interval = serializer.FromDtoResult(QuantityDto.Create(20.0d, "celsius_interval", "temperature_interval"), registry);
        QuantityConstructionResult intervalAsPoint = serializer.FromDtoResult(QuantityDto.Create(20.0d, "celsius_interval", "temperature"), registry);
        QuantityConstructionResult pointAsInterval = serializer.FromDtoResult(QuantityDto.Create(20.0d, "degree_celsius", "temperature_interval"), registry);

        Assert.That(point.IsSuccess(), Is.True, point.GetMessage());
        Assert.That(interval.IsSuccess(), Is.True, interval.GetMessage());
        AssertIncompatibleKind(intervalAsPoint, "temperature");
        AssertIncompatibleKind(pointAsInterval, "temperature_interval");
    }

    [Test]
    public void NonThrowingCanonicalDeserialization_ReturnsEveryExpectedFailureCategory()
    {
        UnitRegistry standard = new StandardRegistry().Create();
        UnitRegistry ambiguous = CreateAmbiguousUnitRegistry();
        QuantitySerializer serializer = new QuantitySerializer();

        AssertParseFailure(serializer.DeserializeCanonicalResult(null!, standard), UnitErrorCode.ParseInvalidInput);
        AssertParseFailure(serializer.DeserializeCanonicalResult("", standard), UnitErrorCode.ParseInvalidInput);
        AssertParseFailure(serializer.DeserializeCanonicalResult(" ", standard), UnitErrorCode.ParseInvalidInput);
        AssertParseFailure(serializer.DeserializeCanonicalResult("1 meter", null!), UnitErrorCode.ParseInvalidInput);
        AssertParseFailure(serializer.DeserializeCanonicalResult(" 1 meter", standard), UnitErrorCode.ParseInvalidInput);
        AssertParseFailure(serializer.DeserializeCanonicalResult("1 meter ", standard), UnitErrorCode.ParseInvalidInput);
        AssertParseFailure(serializer.DeserializeCanonicalResult("1meter", standard), UnitErrorCode.ParseInvalidInput);
        AssertParseFailure(serializer.DeserializeCanonicalResult("value meter", standard), UnitErrorCode.ParseInvalidInput);
        AssertParseFailure(serializer.DeserializeCanonicalResult("1 (meter/)", standard), UnitErrorCode.ParseInvalidInput);
        AssertParseFailure(serializer.DeserializeCanonicalResult("1 unresolved", standard), UnitErrorCode.ParseUnknownUnit);
        AssertParseFailure(serializer.DeserializeCanonicalResult("1 u", ambiguous), UnitErrorCode.ParseAmbiguousUnit);
    }

    [Test]
    public void NonThrowingDtoDeserialization_ReturnsEveryExpectedFailureCategoryAndCandidates()
    {
        UnitRegistry standard = new StandardRegistry().Create();
        UnitRegistry ambiguousUnits = CreateAmbiguousUnitRegistry();
        UnitRegistry ambiguousKinds = CreateAmbiguousKindRegistry();
        QuantitySerializer serializer = new QuantitySerializer();
        QuantityDto nullUnit = new QuantityDto();
        nullUnit.Unit = null!;

        AssertConstructionFailure(serializer.FromDtoResult(null!, standard), UnitErrorCode.ParseInvalidInput);
        AssertConstructionFailure(serializer.FromDtoResult(new QuantityDto(), null!), UnitErrorCode.ParseInvalidInput);
        AssertConstructionFailure(serializer.FromDtoResult(new QuantityDto(), standard), UnitErrorCode.ParseInvalidInput);
        AssertConstructionFailure(serializer.FromDtoResult(nullUnit, standard), UnitErrorCode.ParseInvalidInput);
        AssertConstructionFailure(serializer.FromDtoResult(QuantityDto.Create(1.0d, "(meter/)", ""), standard), UnitErrorCode.ParseInvalidInput);
        AssertConstructionFailure(serializer.FromDtoResult(QuantityDto.Create(1.0d, "unresolved", ""), standard), UnitErrorCode.ParseUnknownUnit);
        QuantityConstructionResult ambiguousUnit = serializer.FromDtoResult(QuantityDto.Create(1.0d, "u", ""), ambiguousUnits);
        AssertConstructionFailure(ambiguousUnit, UnitErrorCode.ParseAmbiguousUnit);
        Assert.That(ambiguousUnit.GetCandidates(), Has.Length.EqualTo(2));
        AssertConstructionFailure(serializer.FromDtoResult(QuantityDto.Create(1.0d, "meter", "unresolved_kind"), standard), UnitErrorCode.LookupNotFound);
        AssertConstructionFailure(serializer.FromDtoResult(QuantityDto.Create(1.0d, "meter", "shared kind"), ambiguousKinds), UnitErrorCode.LookupAmbiguous);
        AssertConstructionFailure(serializer.FromDtoResult(QuantityDto.Create(1.0d, "second", "length"), standard), UnitErrorCode.ConversionIncompatibleDimensions);
    }

    static UnitDefinition CustomLengthUnit(string id, string name, string pluralName, string symbol, RationalNumber scale, bool allowsPrefixes)
    {
        return new UnitDefinition(
            id,
            name,
            pluralName,
            symbol,
            new string[0],
            new StandardDimensions().Length(),
            scale,
            new RationalNumber(0, 1),
            allowsPrefixes,
            UnitConversionKind.Scale);
    }

    static UnitRegistry CreateAmbiguousUnitRegistry()
    {
        UnitRegistry registry = new UnitRegistry();
        registry.AddUnit(CustomLengthUnit("alpha", "alpha", "alphas", "u", new RationalNumber(1, 1), false));
        registry.AddUnit(CustomLengthUnit("beta", "beta", "betas", "u", new RationalNumber(1, 1), false));
        return registry;
    }

    static UnitRegistry CreateAmbiguousKindRegistry()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition meter = new StandardUnits().Meter();
        registry.AddUnit(meter);
        registry.AddQuantityKind(new QuantityKindDefinition(
            "first_length", "First length", new StandardDimensions().Length(), "meter", new string[] { "shared kind" }, new string[0]));
        registry.AddQuantityKind(new QuantityKindDefinition(
            "second_length", "Second length", new StandardDimensions().Length(), "meter", new string[] { "shared kind" }, new string[0]));
        return registry;
    }

    static void AssertParseFailure(QuantityParseResult result, UnitErrorCode errorCode)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(errorCode), result.GetMessage());
        Assert.Throws<System.InvalidOperationException>(() => result.GetQuantity());
    }

    static void AssertConstructionFailure(QuantityConstructionResult result, UnitErrorCode errorCode)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(errorCode), result.GetMessage());
        Assert.Throws<System.InvalidOperationException>(() => result.GetQuantity());
    }

    static void AssertIncompatibleKind(QuantityConstructionResult result, string quantityKindId)
    {
        AssertConstructionFailure(result, UnitErrorCode.ConversionIncompatibleDimensions);
        Assert.That(result.GetMessage(), Does.Contain(quantityKindId));
        Assert.That(result.GetCandidates(), Is.Empty);
    }
}
