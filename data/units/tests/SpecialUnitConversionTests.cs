using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class SpecialUnitConversionTests
{
    [Test]
    public void ConvertSpecial_ConvertsBase10SpecialUnitThroughSystemValue()
    {
        IUnitConverter converter = new UnitConverter();
        UnitDefinition decibelMilliwatt = DecibelMilliwatt();
        UnitDefinition watt = Watt();
        UnitDefinition milliwatt = Milliwatt();
        SpecialConversionDefinition metadata = DecibelMilliwattMetadata();

        ConversionResult zeroDbm = converter.ConvertSpecial(0.0d, decibelMilliwatt, milliwatt, metadata);
        ConversionResult thirtyDbm = converter.ConvertSpecial(30.0d, decibelMilliwatt, watt, metadata);
        ConversionResult oneWatt = converter.ConvertSpecial(1.0d, watt, decibelMilliwatt, metadata);

        AssertSuccess(zeroDbm, 1.0d, milliwatt);
        AssertSuccess(thirtyDbm, 1.0d, watt);
        AssertSuccess(oneWatt, 30.0d, decibelMilliwatt);
    }

    [Test]
    public void ConvertSpecial_ConvertsNaturalLogarithmicStrategy()
    {
        UnitConverter converter = new UnitConverter();
        UnitDefinition neper = Neper();
        UnitDefinition ratio = Ratio();
        SpecialConversionDefinition metadata = new SpecialConversionDefinition("neper_ratio", SpecialConversionStrategy.NaturalLogarithmicRatio, new RationalNumber(1, 1), new RationalNumber(1, 1));

        ConversionResult linear = converter.ConvertSpecial(1.0d, neper, ratio, metadata);
        ConversionResult logarithmic = converter.ConvertSpecial(System.Math.E, ratio, neper, metadata);

        AssertSuccess(linear, System.Math.E, ratio);
        AssertSuccess(logarithmic, 1.0d, neper);
    }

    [Test]
    public void ConvertSpecial_PreservesValueBetweenEquivalentSpecialUnitDefinitions()
    {
        UnitConverter converter = new UnitConverter();
        UnitDefinition source = DecibelMilliwatt();
        UnitDefinition target = DecibelMilliwatt();

        ConversionResult result = converter.ConvertSpecial(12.25d, source, target, DecibelMilliwattMetadata());

        AssertSuccess(result, 12.25d, target);
    }

    [Test]
    public void GetSpecialPlan_ReturnsSupportedPlanWithExplicitMetadata()
    {
        UnitConverter converter = new UnitConverter();
        SpecialConversionDefinition metadata = DecibelMilliwattMetadata();

        ConversionPlan plan = converter.GetSpecialPlan(DecibelMilliwatt(), Watt(), metadata);

        Assert.That(plan.IsSupported(), Is.True);
        Assert.That(plan.GetConversionKind(), Is.EqualTo(UnitConversionKind.Special));
        Assert.That(plan.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(plan.HasSpecialConversion(), Is.True);
        Assert.That(plan.GetSpecialConversion(), Is.SameAs(metadata));
        Assert.That(plan.GetValueScale().EqualsValue(new RationalNumber(1, 1)), Is.True);
        Assert.That(plan.GetValueOffset().IsZero(), Is.True);
    }

    [Test]
    public void ConvertSpecial_ReturnsFailureForNonPositiveLogarithmInput()
    {
        UnitConverter converter = new UnitConverter();

        ConversionResult zero = converter.ConvertSpecial(0.0d, Watt(), DecibelMilliwatt(), DecibelMilliwattMetadata());
        ConversionResult negative = converter.ConvertSpecial(-1.0d, Watt(), DecibelMilliwatt(), DecibelMilliwattMetadata());

        Assert.That(zero.IsFailure(), Is.True);
        Assert.That(zero.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
        Assert.That(zero.GetMessage(), Does.Contain("positive"));
        Assert.That(negative.IsFailure(), Is.True);
    }

    [Test]
    public void GetSpecialPlan_RejectsMismatchedIncompatibleAndInvalidUnitMetadata()
    {
        UnitConverter converter = new UnitConverter();
        UnitDefinition scaledSpecial = new UnitDefinition("decibel_milliwatt", "decibel-milliwatt", "decibel-milliwatts", "dBm", new string[0], PowerDimension(), new RationalNumber(2, 1), new RationalNumber(0, 1), false, UnitConversionKind.Special);
        UnitDefinition affinePower = new UnitDefinition("affine_power", "affine power", "affine power", "ap", new string[0], PowerDimension(), new RationalNumber(1, 1), new RationalNumber(1, 1), false, UnitConversionKind.Affine);
        UnitDefinition otherSpecial = new UnitDefinition("other_special", "other special", "other specials", "osp", new string[0], PowerDimension(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Special);

        ConversionPlan mismatch = converter.GetSpecialPlan(DecibelMilliwatt(), Watt(), new SpecialConversionDefinition("other", SpecialConversionStrategy.Base10LogarithmicRatio, new RationalNumber(1, 1), new RationalNumber(10, 1)));
        ConversionPlan incompatible = converter.GetSpecialPlan(DecibelMilliwatt(), Meter(), DecibelMilliwattMetadata());
        ConversionPlan invalidSpecial = converter.GetSpecialPlan(scaledSpecial, Watt(), DecibelMilliwattMetadata());
        ConversionPlan invalidPeer = converter.GetSpecialPlan(DecibelMilliwatt(), affinePower, DecibelMilliwattMetadata());
        ConversionPlan differentSpecial = converter.GetSpecialPlan(DecibelMilliwatt(), otherSpecial, DecibelMilliwattMetadata());

        Assert.That(mismatch.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
        Assert.That(incompatible.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(invalidSpecial.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
        Assert.That(invalidPeer.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
        Assert.That(differentSpecial.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
    }

    [Test]
    public void CanConvertSpecial_ReportsAvailabilityWithoutThrowingForMissingInputs()
    {
        UnitConverter converter = new UnitConverter();
        UnitDefinition special = DecibelMilliwatt();
        UnitDefinition watt = Watt();
        SpecialConversionDefinition metadata = DecibelMilliwattMetadata();

        Assert.That(converter.CanConvertSpecial(special, watt, metadata), Is.True);
        Assert.That(converter.CanConvertSpecial(watt, special, metadata), Is.True);
        Assert.That(converter.CanConvertSpecial(null, watt, metadata), Is.False);
        Assert.That(converter.CanConvertSpecial(special, null, metadata), Is.False);
        Assert.That(converter.CanConvertSpecial(special, watt, null), Is.False);
        Assert.That(converter.CanConvertSpecial(special, Meter(), metadata), Is.False);
    }

    [Test]
    public void OrdinaryConversion_RemainsSeparateFromSpecialAndCrossDimensionEquivalencyPaths()
    {
        UnitConverter converter = new UnitConverter();

        ConversionResult ordinarySpecial = converter.Convert(0.0d, DecibelMilliwatt(), Watt());
        ConversionPlan crossDimensionSpecial = converter.GetSpecialPlan(DecibelMilliwatt(), Meter(), DecibelMilliwattMetadata());

        Assert.That(ordinarySpecial.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
        Assert.That(converter.CanConvert(DecibelMilliwatt(), Watt()), Is.False);
        Assert.That(crossDimensionSpecial.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
    }

    [Test]
    public void SpecialMethods_RejectMissingRequiredInputs()
    {
        UnitConverter converter = new UnitConverter();
        UnitDefinition special = DecibelMilliwatt();
        UnitDefinition watt = Watt();
        SpecialConversionDefinition metadata = DecibelMilliwattMetadata();

        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertSpecial(1.0d, null!, watt, metadata));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertSpecial(1.0d, special, null!, metadata));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertSpecial(1.0d, special, watt, null!));
        Assert.Throws<System.ArgumentNullException>(() => converter.GetSpecialPlan(null!, watt, metadata));
        Assert.Throws<System.ArgumentNullException>(() => converter.GetSpecialPlan(special, null!, metadata));
        Assert.Throws<System.ArgumentNullException>(() => converter.GetSpecialPlan(special, watt, null!));
    }

    static void AssertSuccess(ConversionResult result, double value, UnitDefinition unit)
    {
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue(), Is.EqualTo(value).Within(0.000000001d));
        Assert.That(result.GetUnit(), Is.SameAs(unit));
    }

    static SpecialConversionDefinition DecibelMilliwattMetadata()
    {
        return new SpecialConversionDefinition("decibel_milliwatt", SpecialConversionStrategy.Base10LogarithmicRatio, new RationalNumber(1, 1000), new RationalNumber(10, 1));
    }

    static UnitDefinition DecibelMilliwatt()
    {
        return new UnitDefinition("decibel_milliwatt", "decibel-milliwatt", "decibel-milliwatts", "dBm", new string[0], PowerDimension(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Special);
    }

    static UnitDefinition Neper()
    {
        return new UnitDefinition("neper_ratio", "neper ratio", "neper ratios", "Np", new string[0], DimensionVector.Dimensionless(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Special);
    }

    static UnitDefinition Watt()
    {
        return new UnitDefinition("watt", "watt", "watts", "W", new string[0], PowerDimension(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Scale);
    }

    static UnitDefinition Milliwatt()
    {
        return new UnitDefinition("milliwatt", "milliwatt", "milliwatts", "mW", new string[0], PowerDimension(), new RationalNumber(1, 1000), new RationalNumber(0, 1), false, UnitConversionKind.Scale);
    }

    static UnitDefinition Ratio()
    {
        return new UnitDefinition("ratio", "ratio", "ratios", "1", new string[0], DimensionVector.Dimensionless(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Scale);
    }

    static UnitDefinition Meter()
    {
        return new UnitDefinition("meter", "meter", "meters", "m", new string[0], DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Scale);
    }

    static DimensionVector PowerDimension()
    {
        return new StandardUnits().Watt().GetDimension();
    }
}
