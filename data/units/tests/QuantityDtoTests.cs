using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityDtoTests
{
    [Test]
    public void DefaultConstructor_InitializesSerializationFriendlyFields()
    {
        QuantityDto dto = new QuantityDto();

        Assert.That(dto.Value, Is.EqualTo(0.0d));
        Assert.That(dto.Unit, Is.EqualTo(""));
        Assert.That(dto.QuantityKind, Is.EqualTo(""));

        dto.Value = 12.5d;
        dto.Unit = "meter";
        dto.QuantityKind = "length";

        Assert.That(dto.GetValue(), Is.EqualTo(12.5d));
        Assert.That(dto.GetUnit(), Is.EqualTo("meter"));
        Assert.That(dto.GetQuantityKind(), Is.EqualTo("length"));
        Assert.That(dto.HasQuantityKind(), Is.True);
    }

    [Test]
    public void Create_StoresValueUnitAndOptionalQuantityKind()
    {
        QuantityDto withKind = QuantityDto.Create(9.81d, "(meter/(second^2))", "acceleration");
        QuantityDto withoutKind = QuantityDto.Create(1.0d, "meter", null);

        Assert.That(withKind.GetValue(), Is.EqualTo(9.81d));
        Assert.That(withKind.GetUnit(), Is.EqualTo("(meter/(second^2))"));
        Assert.That(withKind.GetQuantityKind(), Is.EqualTo("acceleration"));
        Assert.That(withKind.HasQuantityKind(), Is.True);
        Assert.That(withoutKind.GetQuantityKind(), Is.EqualTo(""));
        Assert.That(withoutKind.HasQuantityKind(), Is.False);
    }

    [Test]
    public void FromQuantity_CreatesDtoFromQuantityValue()
    {
        QuantityValue quantity = new QuantityValue(3.5d, new StandardUnits().Foot());

        QuantityDto withoutKind = QuantityDto.FromQuantity(quantity);
        QuantityDto withKind = QuantityDto.FromQuantity(quantity, "length");

        Assert.That(withoutKind.Value, Is.EqualTo(3.5d));
        Assert.That(withoutKind.Unit, Is.EqualTo("foot"));
        Assert.That(withoutKind.QuantityKind, Is.EqualTo(""));
        Assert.That(withKind.Value, Is.EqualTo(3.5d));
        Assert.That(withKind.Unit, Is.EqualTo("foot"));
        Assert.That(withKind.QuantityKind, Is.EqualTo("length"));
    }

    [Test]
    public void ToQuantity_ResolvesUnitIdThroughRegistry()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityDto dto = QuantityDto.Create(2.0d, "mile", "length");

        QuantityValue quantity = dto.ToQuantity(registry);

        Assert.That(quantity.GetValue(), Is.EqualTo(2.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("mile"));
        Assert.That(quantity.As(new StandardUnits().Foot()), Is.EqualTo(10560.0d).Within(0.000000001d));
    }

    [Test]
    public void PublicFields_RoundTripWithBuiltInJsonSerializerWhenFieldsAreIncluded()
    {
        QuantityDto dto = QuantityDto.Create(1.25d, "kilogram", "mass");
        System.Text.Json.JsonSerializerOptions options = new System.Text.Json.JsonSerializerOptions();
        options.IncludeFields = true;

        string json = System.Text.Json.JsonSerializer.Serialize(dto, options);
        QuantityDto roundTrip = System.Text.Json.JsonSerializer.Deserialize<QuantityDto>(json, options)
            ?? throw new System.InvalidOperationException("DTO JSON deserialized to null.");

        Assert.That(json, Does.Contain("Value"));
        Assert.That(json, Does.Contain("Unit"));
        Assert.That(json, Does.Contain("QuantityKind"));
        Assert.That(roundTrip, Is.Not.Null);
        Assert.That(roundTrip.Value, Is.EqualTo(1.25d));
        Assert.That(roundTrip.Unit, Is.EqualTo("kilogram"));
        Assert.That(roundTrip.QuantityKind, Is.EqualTo("mass"));
    }

    [Test]
    public void CreateAndFromQuantity_RejectMissingRequiredValues()
    {
        Assert.Throws<System.ArgumentException>(() => QuantityDto.Create(1.0d, "", "length"));
        Assert.Throws<System.ArgumentException>(() => QuantityDto.Create(1.0d, " ", "length"));
        Assert.Throws<System.ArgumentNullException>(() => QuantityDto.FromQuantity(null!));
    }

    [Test]
    public void ToQuantity_RejectsMissingRegistryOrUnknownUnit()
    {
        UnitRegistry registry = new StandardRegistry().Create();
        QuantityDto missingUnit = new QuantityDto();
        QuantityDto unknownUnit = QuantityDto.Create(1.0d, "missing", "");

        Assert.Throws<System.ArgumentNullException>(() => unknownUnit.ToQuantity(null!));
        Assert.Throws<System.ArgumentException>(() => missingUnit.ToQuantity(registry));
        Assert.Throws<System.InvalidOperationException>(() => unknownUnit.ToQuantity(registry));
    }
}
