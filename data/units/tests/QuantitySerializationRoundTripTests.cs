using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantitySerializationRoundTripTests
{
    [Test]
    public void CanonicalStrings_RoundTripRepresentativeQuantities()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        AssertCanonicalRoundTrip(serializer, registry, new QuantityValue(1234.567d, new StandardUnits().Meter()), "1234.567 meter");
        AssertCanonicalRoundTrip(serializer, registry, new QuantityValue(-2.5d, new StandardUnits().Kilogram()), "-2.5 kilogram");
        AssertCanonicalRoundTrip(serializer, registry, new QuantityValue(9.80665d, new StandardUnits().Meter().Divide(new StandardUnits().Second().Pow(2))), "9.80665 (meter/(second^2))");
        AssertCanonicalRoundTrip(serializer, registry, new QuantityValue(1.25d, new StandardUnits().Joule().Multiply(new StandardUnits().Second())), "1.25 (joule*second)");
        AssertCanonicalRoundTrip(serializer, registry, new QuantityValue(1.5d, new StandardUnits().Bit().Divide(new StandardUnits().Second())), "1.5 (bit/second)");
    }

    [Test]
    public void Dtos_RoundTripRepresentativeQuantities()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        AssertDtoRoundTrip(serializer, registry, new QuantityValue(3.0d, new StandardUnits().Foot()), "length");
        AssertDtoRoundTrip(serializer, registry, new QuantityValue(12.0d, new StandardUnits().Meter().Divide(new StandardUnits().Second())), "speed");
        AssertDtoRoundTrip(serializer, registry, new QuantityValue(1.0d, new StandardUnits().Mebibyte()), "data_size");
        AssertDtoRoundTrip(serializer, registry, new QuantityValue(32.0d, new StandardUnits().Foot().Divide(new StandardUnits().Second().Pow(2))), "acceleration");
    }

    [Test]
    public void DeserializeCanonical_RejectsInvalidCanonicalPayloads()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        Assert.Throws<System.ArgumentException>(() => serializer.DeserializeCanonical(" 1 meter", registry));
        Assert.Throws<System.ArgumentException>(() => serializer.DeserializeCanonical("1 meter ", registry));
        Assert.Throws<System.ArgumentException>(() => serializer.DeserializeCanonical("1meter", registry));
        Assert.Throws<System.ArgumentException>(() => serializer.DeserializeCanonical("abc meter", registry));
        Assert.Throws<System.InvalidOperationException>(() => serializer.DeserializeCanonical("1 missing_unit", registry));
        Assert.Throws<System.InvalidOperationException>(() => serializer.DeserializeCanonical("1 (meter/unknown)", registry));
    }

    [Test]
    public void FromDto_RejectsInvalidDtoPayloads()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        Assert.Throws<System.ArgumentException>(() => serializer.FromDto(new QuantityDto(), registry));
        Assert.Throws<System.InvalidOperationException>(() => serializer.FromDto(QuantityDto.Create(1.0d, "missing_unit", ""), registry));
        Assert.Throws<System.InvalidOperationException>(() => serializer.FromDto(QuantityDto.Create(1.0d, "(meter/unknown)", ""), registry));
        Assert.Throws<System.InvalidOperationException>(() => serializer.FromDto(QuantityDto.Create(1.0d, "meter", "missing_kind"), registry));
        Assert.Throws<System.InvalidOperationException>(() => serializer.FromDto(QuantityDto.Create(1.0d, "second", "length"), registry));
    }

    [Test]
    public void JsonDtoPayloads_RoundTripAndRejectMissingRequiredFields()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();
        System.Text.Json.JsonSerializerOptions options = new System.Text.Json.JsonSerializerOptions();
        options.IncludeFields = true;

        string json = System.Text.Json.JsonSerializer.Serialize(QuantityDto.Create(5.0d, "meter", "length"), options);
        QuantityDto validDto = System.Text.Json.JsonSerializer.Deserialize<QuantityDto>(json, options)
            ?? throw new System.InvalidOperationException("Valid DTO JSON deserialized to null.");
        QuantityDto missingUnitDto = System.Text.Json.JsonSerializer.Deserialize<QuantityDto>("{\"Value\":5,\"QuantityKind\":\"length\"}", options)
            ?? throw new System.InvalidOperationException("DTO JSON deserialized to null.");

        Assert.That(validDto, Is.Not.Null);
        Assert.That(serializer.FromDto(validDto, registry).GetUnit().GetId(), Is.EqualTo("meter"));
        Assert.That(missingUnitDto, Is.Not.Null);
        Assert.Throws<System.ArgumentException>(() => serializer.FromDto(missingUnitDto, registry));
        Assert.Throws<System.Text.Json.JsonException>(() => System.Text.Json.JsonSerializer.Deserialize<QuantityDto>("{", options));
    }

    static void AssertCanonicalRoundTrip(QuantitySerializer serializer, UnitRegistry registry, QuantityValue quantity, string expectedText)
    {
        string text = serializer.SerializeCanonical(quantity);
        QuantityValue roundTrip = serializer.DeserializeCanonical(text, registry);

        Assert.That(text, Is.EqualTo(expectedText));
        Assert.That(roundTrip.GetValue(), Is.EqualTo(quantity.GetValue()).Within(System.Math.Abs(quantity.GetValue()) * 0.000000000001d + 0.000000000001d));
        Assert.That(roundTrip.GetUnit().IsEquivalentTo(quantity.GetUnit()), Is.True);
    }

    static void AssertDtoRoundTrip(QuantitySerializer serializer, UnitRegistry registry, QuantityValue quantity, string quantityKind)
    {
        QuantityDto dto = serializer.ToDtoWithQuantityKind(quantity, quantityKind);
        QuantityValue roundTrip = serializer.FromDto(dto, registry);

        Assert.That(dto.QuantityKind, Is.EqualTo(quantityKind));
        Assert.That(roundTrip.GetValue(), Is.EqualTo(quantity.GetValue()).Within(System.Math.Abs(quantity.GetValue()) * 0.000000000001d + 0.000000000001d));
        Assert.That(roundTrip.GetUnit().IsEquivalentTo(quantity.GetUnit()), Is.True);
    }
}
