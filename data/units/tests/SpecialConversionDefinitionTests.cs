using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class SpecialConversionDefinitionTests
{
    [Test]
    public void Base10LogarithmicRatio_ConvertsInBothDirections()
    {
        ISpecialConversionDefinition definition = new SpecialConversionDefinition(
            "decibel_milliwatt",
            SpecialConversionStrategy.Base10LogarithmicRatio,
            new RationalNumber(1, 1000),
            new RationalNumber(10, 1));

        Assert.That(definition.GetUnitId(), Is.EqualTo("decibel_milliwatt"));
        Assert.That(definition.GetStrategy(), Is.EqualTo(SpecialConversionStrategy.Base10LogarithmicRatio));
        Assert.That(definition.GetReferenceSystemValue().EqualsValue(new RationalNumber(1, 1000)), Is.True);
        Assert.That(definition.GetCoefficient().EqualsValue(new RationalNumber(10, 1)), Is.True);
        Assert.That(definition.ConvertToSystemValue(0.0d), Is.EqualTo(0.001d).Within(0.000000000001d));
        Assert.That(definition.ConvertToSystemValue(30.0d), Is.EqualTo(1.0d).Within(0.000000000001d));
        Assert.That(definition.ConvertFromSystemValue(1.0d), Is.EqualTo(30.0d).Within(0.000000000001d));
    }

    [Test]
    public void NaturalLogarithmicRatio_ConvertsInBothDirections()
    {
        SpecialConversionDefinition definition = new SpecialConversionDefinition(
            "neper_ratio",
            SpecialConversionStrategy.NaturalLogarithmicRatio,
            new RationalNumber(1, 1),
            new RationalNumber(1, 1));

        Assert.That(definition.ConvertToSystemValue(1.0d), Is.EqualTo(System.Math.E).Within(0.000000000001d));
        Assert.That(definition.ConvertFromSystemValue(System.Math.E), Is.EqualTo(1.0d).Within(0.000000000001d));
    }

    [Test]
    public void NegativeCoefficient_SupportsInverseLogarithmicScale()
    {
        SpecialConversionDefinition definition = new SpecialConversionDefinition(
            "negative_log_concentration",
            SpecialConversionStrategy.Base10LogarithmicRatio,
            new RationalNumber(1, 1),
            new RationalNumber(-1, 1));

        Assert.That(definition.ConvertToSystemValue(7.0d), Is.EqualTo(0.0000001d).Within(0.000000000000001d));
        Assert.That(definition.ConvertFromSystemValue(0.0000001d), Is.EqualTo(7.0d).Within(0.000000000001d));
    }

    [Test]
    public void MatchesUnit_RequiresConfiguredIdAndSpecialConversionKind()
    {
        SpecialConversionDefinition definition = CreateDefinition();
        UnitDefinition matching = Unit("decibel_milliwatt", UnitConversionKind.Special);
        UnitDefinition ordinarySameId = Unit("decibel_milliwatt", UnitConversionKind.Scale);
        UnitDefinition otherSpecial = Unit("other", UnitConversionKind.Special);

        Assert.That(definition.MatchesUnit(matching), Is.True);
        Assert.That(definition.MatchesUnit(ordinarySameId), Is.False);
        Assert.That(definition.MatchesUnit(otherSpecial), Is.False);
        Assert.That(definition.MatchesUnit(null), Is.False);
    }

    [Test]
    public void Constructor_RejectsInvalidMetadata()
    {
        Assert.Throws<System.ArgumentException>(() => new SpecialConversionDefinition("", SpecialConversionStrategy.Base10LogarithmicRatio, new RationalNumber(1, 1), new RationalNumber(1, 1)));
        Assert.Throws<System.ArgumentException>(() => new SpecialConversionDefinition("special", (SpecialConversionStrategy)0, new RationalNumber(1, 1), new RationalNumber(1, 1)));
        Assert.Throws<System.ArgumentNullException>(() => new SpecialConversionDefinition("special", SpecialConversionStrategy.Base10LogarithmicRatio, null!, new RationalNumber(1, 1)));
        Assert.Throws<System.ArgumentException>(() => new SpecialConversionDefinition("special", SpecialConversionStrategy.Base10LogarithmicRatio, new RationalNumber(0, 1), new RationalNumber(1, 1)));
        Assert.Throws<System.ArgumentException>(() => new SpecialConversionDefinition("special", SpecialConversionStrategy.Base10LogarithmicRatio, new RationalNumber(-1, 1), new RationalNumber(1, 1)));
        Assert.Throws<System.ArgumentNullException>(() => new SpecialConversionDefinition("special", SpecialConversionStrategy.Base10LogarithmicRatio, new RationalNumber(1, 1), null!));
        Assert.Throws<System.ArgumentException>(() => new SpecialConversionDefinition("special", SpecialConversionStrategy.Base10LogarithmicRatio, new RationalNumber(1, 1), new RationalNumber(0, 1)));
    }

    [Test]
    public void ConvertFromSystemValue_RejectsNonPositiveAndNanValues()
    {
        SpecialConversionDefinition definition = CreateDefinition();

        Assert.Throws<System.ArgumentException>(() => definition.ConvertFromSystemValue(0.0d));
        Assert.Throws<System.ArgumentException>(() => definition.ConvertFromSystemValue(-1.0d));
        Assert.Throws<System.ArgumentException>(() => definition.ConvertFromSystemValue(double.NaN));
    }

    static SpecialConversionDefinition CreateDefinition()
    {
        return new SpecialConversionDefinition("decibel_milliwatt", SpecialConversionStrategy.Base10LogarithmicRatio, new RationalNumber(1, 1000), new RationalNumber(10, 1));
    }

    static UnitDefinition Unit(string id, UnitConversionKind conversionKind)
    {
        return new UnitDefinition(id, id, id, id, new string[0], new StandardUnits().Watt().GetDimension(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, conversionKind);
    }
}
