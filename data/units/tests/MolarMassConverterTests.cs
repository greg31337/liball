using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class MolarMassConverterTests
{
    [Test]
    public void Convert_UsesExplicitContextFromAmountToMass()
    {
        MolarMassConverter converter = new MolarMassConverter();

        ConversionResult result = converter.Convert(new QuantityValue(2.0d, new StandardUnits().Mole()), new StandardUnits().Gram(), WaterContext());

        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetValue(), Is.EqualTo(36.03056d).Within(0.000000000001d));
        Assert.That(result.GetUnit().GetId(), Is.EqualTo("gram"));
    }

    [Test]
    public void Convert_UsesExplicitContextFromMassToAmount()
    {
        MolarMassConverter converter = new MolarMassConverter();

        ConversionResult result = converter.Convert(new QuantityValue(36.03056d, new StandardUnits().Gram()), new StandardUnits().Mole(), WaterContext());

        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetValue(), Is.EqualTo(2.0d).Within(0.000000000001d));
        Assert.That(result.GetUnit().GetId(), Is.EqualTo("mole"));
    }

    [Test]
    public void ConvertValue_AppliesSourceTargetAndContextScales()
    {
        MolarMassConverter converter = new MolarMassConverter();
        UnitDefinition millimole = Millimole();

        ConversionResult mass = converter.ConvertValue(500.0d, millimole, new StandardUnits().Gram(), WaterContext());
        ConversionResult amount = converter.ConvertValue(9.00764d, new StandardUnits().Gram(), millimole, WaterContext());

        Assert.That(mass.IsSuccess(), Is.True, mass.GetMessage());
        Assert.That(mass.GetValue(), Is.EqualTo(9.00764d).Within(0.000000000001d));
        Assert.That(amount.IsSuccess(), Is.True, amount.GetMessage());
        Assert.That(amount.GetValue(), Is.EqualTo(500.0d).Within(0.000000001d));
    }

    [Test]
    public void CanConvert_RequiresOppositeSupportedDimensionsScaleUnitsAndContext()
    {
        MolarMassConverter converter = new MolarMassConverter();
        StandardUnits units = new StandardUnits();
        MolarMassContext context = WaterContext();

        Assert.That(converter.CanConvert(units.Mole(), units.Kilogram(), context), Is.True);
        Assert.That(converter.CanConvert(units.Gram(), units.Mole(), context), Is.True);
        Assert.That(converter.CanConvert(units.Mole(), units.Mole(), context), Is.False);
        Assert.That(converter.CanConvert(units.Meter(), units.Kilogram(), context), Is.False);
        Assert.That(converter.CanConvert(AffineAmountUnit(), units.Kilogram(), context), Is.False);
        Assert.That(converter.CanConvert(null, units.Kilogram(), context), Is.False);
        Assert.That(converter.CanConvert(units.Mole(), null, context), Is.False);
        Assert.That(converter.CanConvert(units.Mole(), units.Kilogram(), null), Is.False);
    }

    [Test]
    public void Convert_ReturnsFailuresForIncompatibleDimensionsAndAffineUnits()
    {
        MolarMassConverter converter = new MolarMassConverter();
        MolarMassContext context = WaterContext();

        ConversionResult incompatible = converter.Convert(new QuantityValue(1.0d, new StandardUnits().Meter()), new StandardUnits().Kilogram(), context);
        ConversionResult unsupported = converter.Convert(new QuantityValue(1.0d, AffineAmountUnit()), new StandardUnits().Kilogram(), context);

        Assert.That(incompatible.IsFailure(), Is.True);
        Assert.That(incompatible.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(unsupported.IsFailure(), Is.True);
        Assert.That(unsupported.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
    }

    [Test]
    public void Convert_RejectsMissingRequiredInputs()
    {
        MolarMassConverter converter = new MolarMassConverter();
        QuantityValue amount = new QuantityValue(1.0d, new StandardUnits().Mole());
        MolarMassContext context = WaterContext();

        Assert.Throws<System.ArgumentNullException>(() => converter.Convert(null!, new StandardUnits().Kilogram(), context));
        Assert.Throws<System.ArgumentNullException>(() => converter.Convert(amount, null!, context));
        Assert.Throws<System.ArgumentNullException>(() => converter.Convert(amount, new StandardUnits().Kilogram(), null!));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertValue(1.0d, null!, new StandardUnits().Kilogram(), context));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertValue(1.0d, new StandardUnits().Mole(), null!, context));
    }

    [Test]
    public void OrdinaryConversion_DoesNotApplyMolarMassImplicitly()
    {
        UnitConverter converter = new UnitConverter();
        QuantityValue amount = new QuantityValue(1.0d, new StandardUnits().Mole());

        ConversionResult direct = converter.Convert(1.0d, new StandardUnits().Mole(), new StandardUnits().Kilogram());
        ConversionResult quantity = amount.ConvertTo(new StandardUnits().Kilogram());

        Assert.That(direct.IsFailure(), Is.True);
        Assert.That(direct.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(quantity.IsFailure(), Is.True);
        Assert.That(quantity.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
    }

    static MolarMassContext WaterContext()
    {
        UnitDefinition gramsPerMole = new StandardUnits().Gram().Divide(new StandardUnits().Mole());
        return new MolarMassContext(new QuantityValue(18.01528d, gramsPerMole));
    }

    static UnitDefinition Millimole()
    {
        return new UnitDefinition(
            "millimole",
            "millimole",
            "millimoles",
            "mmol",
            new string[0],
            new StandardDimensions().AmountOfSubstance(),
            new RationalNumber(1, 1000),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }

    static UnitDefinition AffineAmountUnit()
    {
        return new UnitDefinition(
            "affine_amount",
            "affine amount",
            "affine amounts",
            "aa",
            new string[0],
            new StandardDimensions().AmountOfSubstance(),
            new RationalNumber(1, 1),
            new RationalNumber(1, 1),
            false,
            UnitConversionKind.Affine);
    }
}
