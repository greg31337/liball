using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantitySerializerTests
{
    [Test]
    public void SerializeCanonical_UsesInvariantRoundTripValueAndCanonicalUnit()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        QuantityValue speed = new QuantityValue(1.25d, new StandardUnits().Meter().Divide(new StandardUnits().Second()));
        QuantityValue force = new QuantityValue(2.0d, new StandardUnits().Newton());

        string speedText = serializer.SerializeCanonical(speed);
        string forceText = serializer.SerializeCanonical(force);

        Assert.That(speedText, Is.EqualTo("1.25 (meter/second)"));
        Assert.That(forceText, Is.EqualTo("2 newton"));
    }

    [Test]
    public void DeserializeCanonical_ParsesCanonicalQuantityText()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        QuantityValue speed = serializer.DeserializeCanonical("1.25 (meter/second)", registry);
        QuantityValue force = serializer.DeserializeCanonical("2 newton", registry);

        Assert.That(speed.GetValue(), Is.EqualTo(1.25d));
        Assert.That(speed.GetUnit().GetId(), Is.EqualTo("(meter/second)"));
        Assert.That(force.GetValue(), Is.EqualTo(2.0d));
        Assert.That(force.GetUnit().GetId(), Is.EqualTo("newton"));
    }

    [Test]
    public void CanonicalString_RoundTripsRegisteredAndCompoundQuantities()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityValue original = new QuantityValue(9.80665d, new StandardUnits().Meter().Divide(new StandardUnits().Second().Pow(2)));

        string text = serializer.SerializeCanonical(original);
        QuantityValue roundTrip = serializer.DeserializeCanonical(text, registry);

        Assert.That(text, Is.EqualTo("9.80665 (meter/(second^2))"));
        Assert.That(roundTrip.GetValue(), Is.EqualTo(9.80665d));
        Assert.That(roundTrip.GetUnit().IsEquivalentTo(original.GetUnit()), Is.True);
    }

    [Test]
    public void ToDto_CreatesQuantityDtoWithOptionalQuantityKind()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        QuantityValue quantity = new QuantityValue(3.0d, new StandardUnits().Meter());

        QuantityDto withoutKind = serializer.ToDto(quantity);
        QuantityDto withKind = serializer.ToDtoWithQuantityKind(quantity, "length");

        Assert.That(withoutKind.Value, Is.EqualTo(3.0d));
        Assert.That(withoutKind.Unit, Is.EqualTo("meter"));
        Assert.That(withoutKind.QuantityKind, Is.EqualTo(""));
        Assert.That(withKind.Value, Is.EqualTo(3.0d));
        Assert.That(withKind.Unit, Is.EqualTo("meter"));
        Assert.That(withKind.QuantityKind, Is.EqualTo("length"));
    }

    [Test]
    public void FromDto_ResolvesQuantityAndValidatesQuantityKind()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityDto length = QuantityDto.Create(3.0d, "foot", "length");
        QuantityDto speed = QuantityDto.Create(12.0d, "(meter/second)", "speed");

        QuantityValue lengthQuantity = serializer.FromDto(length, registry);
        QuantityValue speedQuantity = serializer.FromDto(speed, registry);

        Assert.That(lengthQuantity.GetValue(), Is.EqualTo(3.0d));
        Assert.That(lengthQuantity.GetUnit().GetId(), Is.EqualTo("foot"));
        Assert.That(speedQuantity.GetValue(), Is.EqualTo(12.0d));
        Assert.That(speedQuantity.GetUnit().GetId(), Is.EqualTo("(meter/second)"));
    }

    [Test]
    public void FromDto_RejectsUnknownOrIncompatibleQuantityKind()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();

        Assert.Throws<System.InvalidOperationException>(() => serializer.FromDto(QuantityDto.Create(1.0d, "meter", "missing"), registry));
        Assert.Throws<System.InvalidOperationException>(() => serializer.FromDto(QuantityDto.Create(1.0d, "second", "length"), registry));
    }

    [Test]
    public void SerializerMethods_RejectMissingInputs()
    {
        QuantitySerializer serializer = new QuantitySerializer();
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityValue quantity = new QuantityValue(1.0d, new StandardUnits().Meter());
        QuantityDto dto = QuantityDto.Create(1.0d, "meter", "");

        Assert.Throws<System.ArgumentNullException>(() => serializer.SerializeCanonical(null!));
        Assert.Throws<System.ArgumentException>(() => serializer.DeserializeCanonical("", registry));
        Assert.Throws<System.ArgumentNullException>(() => serializer.DeserializeCanonical("1 meter", null!));
        Assert.Throws<System.ArgumentNullException>(() => serializer.ToDto(null!));
        Assert.Throws<System.ArgumentNullException>(() => serializer.ToDtoWithQuantityKind(null!, "length"));
        Assert.Throws<System.ArgumentNullException>(() => serializer.FromDto(null!, registry));
        Assert.Throws<System.ArgumentNullException>(() => serializer.FromDto(dto, null!));
        Assert.That(serializer.ToDtoWithQuantityKind(quantity, null).QuantityKind, Is.EqualTo(""));
    }
}
