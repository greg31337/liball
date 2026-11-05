using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantitySerializerResultTests
{
    [Test]
    public void DeserializeCanonicalResult_ReturnsSuccessWithoutThrowing()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        QuantityParseResult result = serializer.DeserializeCanonicalResult("1.25 (meter/second)", registry);

        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetQuantity().GetValue(), Is.EqualTo(1.25d));
        Assert.That(result.GetQuantity().GetUnit().GetId(), Is.EqualTo("(meter/second)"));
    }

    [Test]
    public void DeserializeCanonicalResult_ReturnsStructuredInputAndLookupFailures()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        QuantityParseResult missingText = serializer.DeserializeCanonicalResult(null!, registry);
        QuantityParseResult missingCatalog = serializer.DeserializeCanonicalResult("1 meter", null!);
        QuantityParseResult malformed = serializer.DeserializeCanonicalResult("1meter", registry);
        QuantityParseResult unknown = serializer.DeserializeCanonicalResult("1 missing_unit", registry);

        Assert.That(missingText.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(missingCatalog.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(malformed.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(unknown.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseUnknownUnit));
        Assert.That(unknown.GetPosition(), Is.EqualTo(2));
    }

    [Test]
    public void FromDtoResult_ReturnsSuccessAndValidatesQuantityKind()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        QuantityConstructionResult result = serializer.FromDtoResult(QuantityDto.Create(3.0d, "foot", "length"), registry);

        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetQuantity().GetValue(), Is.EqualTo(3.0d));
        Assert.That(result.GetQuantity().GetUnit().GetId(), Is.EqualTo("foot"));
    }

    [Test]
    public void FromDtoResult_ReturnsStructuredFailuresWithoutThrowing()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        QuantityConstructionResult missingDto = serializer.FromDtoResult(null!, registry);
        QuantityConstructionResult missingCatalog = serializer.FromDtoResult(new QuantityDto(), null!);
        QuantityConstructionResult missingUnit = serializer.FromDtoResult(new QuantityDto(), registry);
        QuantityConstructionResult unknownUnit = serializer.FromDtoResult(QuantityDto.Create(1.0d, "missing_unit", ""), registry);
        QuantityConstructionResult unknownKind = serializer.FromDtoResult(QuantityDto.Create(1.0d, "meter", "missing_kind"), registry);
        QuantityConstructionResult incompatibleKind = serializer.FromDtoResult(QuantityDto.Create(1.0d, "second", "length"), registry);

        Assert.That(missingDto.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(missingCatalog.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(missingUnit.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseInvalidInput));
        Assert.That(unknownUnit.GetErrorCode(), Is.EqualTo(UnitErrorCode.ParseUnknownUnit));
        Assert.That(unknownKind.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.That(incompatibleKind.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
    }

    [Test]
    public void CanonicalTextAndDto_RoundTripEscapedCustomUnits()
    {
        UnitRegistry registry = new UnitRegistry();
        UnitDefinition custom = new UnitDefinition(
            "widget/2",
            "widget two",
            "widget twos",
            "w2",
            new string[0],
            new StandardDimensions().Length(),
            new RationalNumber(3, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
        registry.AddUnit(custom);
        QuantityValue quantity = new QuantityValue(2.5d, custom);
        QuantitySerializer serializer = new QuantitySerializer();

        string text = serializer.SerializeCanonical(quantity);
        QuantityDto dto = serializer.ToDto(quantity);
        QuantityParseResult textResult = serializer.DeserializeCanonicalResult(text, registry);
        QuantityConstructionResult dtoResult = serializer.FromDtoResult(dto, registry);

        Assert.That(text, Is.EqualTo("2.5 [007700690064006700650074002F0032]"));
        Assert.That(dto.Unit, Is.EqualTo("[007700690064006700650074002F0032]"));
        Assert.That(textResult.IsSuccess(), Is.True, textResult.GetMessage());
        Assert.That(dtoResult.IsSuccess(), Is.True, dtoResult.GetMessage());
        Assert.That(textResult.GetQuantity().GetUnit(), Is.SameAs(custom));
        Assert.That(dtoResult.GetQuantity().GetUnit(), Is.SameAs(custom));
    }

    [Test]
    public void CanonicalText_RoundTripsInvariantNonfiniteValues()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        string nanText = serializer.SerializeCanonical(new QuantityValue(double.NaN, new StandardUnits().Meter()));
        string positiveInfinityText = serializer.SerializeCanonical(new QuantityValue(double.PositiveInfinity, new StandardUnits().Second()));
        string negativeInfinityText = serializer.SerializeCanonical(new QuantityValue(double.NegativeInfinity, new StandardUnits().Kilogram()));
        QuantityParseResult nan = serializer.DeserializeCanonicalResult(nanText, registry);
        QuantityParseResult positiveInfinity = serializer.DeserializeCanonicalResult(positiveInfinityText, registry);
        QuantityParseResult negativeInfinity = serializer.DeserializeCanonicalResult(negativeInfinityText, registry);

        Assert.That(nanText, Is.EqualTo("NaN meter"));
        Assert.That(positiveInfinityText, Is.EqualTo("Infinity second"));
        Assert.That(negativeInfinityText, Is.EqualTo("-Infinity kilogram"));
        Assert.That(double.IsNaN(nan.GetQuantity().GetValue()), Is.True);
        Assert.That(positiveInfinity.GetQuantity().GetValue(), Is.EqualTo(double.PositiveInfinity));
        Assert.That(negativeInfinity.GetQuantity().GetValue(), Is.EqualTo(double.NegativeInfinity));
    }
}
