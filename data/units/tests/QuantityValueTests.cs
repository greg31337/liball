using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityValueTests
{
    [Test]
    public void Constructor_StoresValueAndUnit()
    {
        UnitDefinition meter = new StandardUnits().Meter();

        QuantityValue quantity = new QuantityValue(12.5d, meter);

        Assert.That(quantity.GetValue(), Is.EqualTo(12.5d));
        Assert.That(quantity.GetUnit(), Is.SameAs(meter));
    }

    [Test]
    public void Constructor_RejectsMissingUnit()
    {
        Assert.Throws<System.ArgumentNullException>(() => new QuantityValue(1.0d, null!));
    }

    [Test]
    public void ConvertTo_ReturnsConversionResultWithoutChangingOriginalQuantity()
    {
        QuantityValue quantity = new QuantityValue(12.0d, new StandardUnits().Inch());

        ConversionResult result = quantity.ConvertTo(new StandardUnits().Foot());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue(), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(result.GetUnit().GetId(), Is.EqualTo("foot"));
        Assert.That(quantity.GetValue(), Is.EqualTo(12.0d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo("inch"));
    }

    [Test]
    public void ToUnit_ReturnsConvertedQuantity()
    {
        QuantityValue quantity = new QuantityValue(2.0d, new StandardUnits().Gallon());

        QuantityValue converted = quantity.ToUnit(new StandardUnits().Quart());

        Assert.That(converted.GetValue(), Is.EqualTo(8.0d).Within(0.000000000001d));
        Assert.That(converted.GetUnit().GetId(), Is.EqualTo("us_quart"));
    }

    [Test]
    public void As_ReturnsConvertedValue()
    {
        QuantityValue quantity = new QuantityValue(1.0d, new StandardUnits().Mebibyte());

        double value = quantity.As(new StandardUnits().Byte());

        Assert.That(value, Is.EqualTo(1048576.0d).Within(0.000000000001d));
    }

    [Test]
    public void ConversionHelpers_RejectMissingTargetUnit()
    {
        QuantityValue quantity = new QuantityValue(1.0d, new StandardUnits().Meter());

        Assert.Throws<System.ArgumentNullException>(() => quantity.ConvertTo(null!));
        Assert.Throws<System.ArgumentNullException>(() => quantity.ToUnit(null!));
        Assert.Throws<System.ArgumentNullException>(() => quantity.As(null!));
    }

    [Test]
    public void ConversionHelpers_ReportOrThrowForIncompatibleUnits()
    {
        QuantityValue quantity = new QuantityValue(1.0d, new StandardUnits().Meter());

        ConversionResult result = quantity.ConvertTo(new StandardUnits().Second());

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.Throws<System.InvalidOperationException>(() => quantity.ToUnit(new StandardUnits().Second()));
        Assert.Throws<System.InvalidOperationException>(() => quantity.As(new StandardUnits().Second()));
    }

    [Test]
    public void IsCompatibleWith_ReturnsTrueForCompatibleUnitsAndQuantities()
    {
        QuantityValue length = new QuantityValue(1.0d, new StandardUnits().Meter());
        QuantityValue lengthInFeet = new QuantityValue(1.0d, new StandardUnits().Foot());
        QuantityValue time = new QuantityValue(1.0d, new StandardUnits().Second());

        Assert.That(length.IsCompatibleWithUnit(new StandardUnits().Inch()), Is.True);
        Assert.That(length.IsCompatibleWith(lengthInFeet), Is.True);
        Assert.That(length.IsCompatibleWithUnit(new StandardUnits().Second()), Is.False);
        Assert.That(length.IsCompatibleWith(time), Is.False);
        Assert.That(length.IsCompatibleWithUnit((UnitDefinition?)null), Is.False);
        Assert.That(length.IsCompatibleWith((QuantityValue?)null), Is.False);
    }

    [Test]
    public void IsDimensionless_ReturnsTrueOnlyForDimensionlessUnits()
    {
        QuantityValue ratio = new QuantityValue(0.5d, DimensionlessUnit());
        QuantityValue length = new QuantityValue(1.0d, new StandardUnits().Meter());

        Assert.That(ratio.IsDimensionless(), Is.True);
        Assert.That(length.IsDimensionless(), Is.False);
    }

    static UnitDefinition DimensionlessUnit()
    {
        return new UnitDefinition(
            "one",
            "one",
            "one",
            "1",
            new string[0],
            DimensionVector.Dimensionless(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
