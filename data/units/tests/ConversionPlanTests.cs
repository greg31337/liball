using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class ConversionPlanTests
{
    [Test]
    public void Create_BuildsScalePlanWithCachedScaleAndOffset()
    {
        UnitDefinition meter = CreateUnit("meter", "meter", "meters", "m", DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), UnitConversionKind.Scale);
        UnitDefinition foot = CreateUnit("foot", "foot", "feet", "ft", DimensionVector.Length(), new RationalNumber(381, 1250), new RationalNumber(0, 1), UnitConversionKind.Scale);

        ConversionPlan plan = ConversionPlan.Create(meter, foot);

        Assert.That(plan.IsSupported(), Is.True);
        Assert.That(plan.IsUnsupported(), Is.False);
        Assert.That(plan.GetFromUnit(), Is.SameAs(meter));
        Assert.That(plan.GetToUnit(), Is.SameAs(foot));
        Assert.That(plan.GetConversionKind(), Is.EqualTo(UnitConversionKind.Scale));
        Assert.That(plan.GetValueScale().EqualsValue(new RationalNumber(1250, 381)), Is.True);
        Assert.That(plan.GetValueOffset().EqualsValue(new RationalNumber(0, 1)), Is.True);
        Assert.That(plan.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        Assert.That(plan.HasSpecialConversion(), Is.False);
        Assert.Throws<System.InvalidOperationException>(() => plan.GetSpecialConversion());
    }

    [Test]
    public void Create_BuildsAffinePlanWithCachedScaleAndOffset()
    {
        UnitDefinition celsius = CreateUnit("degree_celsius", "degree Celsius", "degrees Celsius", "degC", DimensionVector.ThermodynamicTemperature(), new RationalNumber(1, 1), new RationalNumber(27315, 100), UnitConversionKind.Affine);
        UnitDefinition fahrenheit = CreateUnit("degree_fahrenheit", "degree Fahrenheit", "degrees Fahrenheit", "degF", DimensionVector.ThermodynamicTemperature(), new RationalNumber(5, 9), new RationalNumber(45967, 180), UnitConversionKind.Affine);

        ConversionPlan plan = ConversionPlan.Create(celsius, fahrenheit);

        Assert.That(plan.IsSupported(), Is.True);
        Assert.That(plan.GetConversionKind(), Is.EqualTo(UnitConversionKind.Affine));
        Assert.That(plan.GetValueScale().EqualsValue(new RationalNumber(9, 5)), Is.True);
        Assert.That(plan.GetValueOffset().EqualsValue(new RationalNumber(32, 1)), Is.True);
    }

    [Test]
    public void Create_ReturnsUnsupportedPlanForIncompatibleDimensions()
    {
        UnitDefinition meter = CreateUnit("meter", "meter", "meters", "m", DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), UnitConversionKind.Scale);
        UnitDefinition second = CreateUnit("second", "second", "seconds", "s", DimensionVector.Time(), new RationalNumber(1, 1), new RationalNumber(0, 1), UnitConversionKind.Scale);

        ConversionPlan plan = ConversionPlan.Create(meter, second);

        Assert.That(plan.IsSupported(), Is.False);
        Assert.That(plan.IsUnsupported(), Is.True);
        Assert.That(plan.GetConversionKind(), Is.EqualTo(UnitConversionKind.Unsupported));
        Assert.That(plan.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
        Assert.That(plan.GetMessage(), Does.Contain("incompatible"));
    }

    [Test]
    public void Create_ReturnsSpecialPlanForSpecialConversionKind()
    {
        UnitDefinition wavelength = CreateUnit("wavelength", "wavelength", "wavelengths", "lambda", DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), UnitConversionKind.Special);
        UnitDefinition length = CreateUnit("meter", "meter", "meters", "m", DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), UnitConversionKind.Scale);

        ConversionPlan plan = ConversionPlan.Create(wavelength, length);

        Assert.That(plan.IsSupported(), Is.False);
        Assert.That(plan.GetConversionKind(), Is.EqualTo(UnitConversionKind.Special));
        Assert.That(plan.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
        Assert.That(plan.GetMessage(), Does.Contain("Special"));
    }

    [Test]
    public void Create_ReturnsUnsupportedPlanForUnsupportedUnitsAndTemperaturePointIntervalMixes()
    {
        UnitDefinition unsupported = CreateUnit("unsupported", "unsupported", "unsupported", "u", DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), UnitConversionKind.Unsupported);
        UnitDefinition meter = CreateUnit("meter", "meter", "meters", "m", DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), UnitConversionKind.Scale);
        UnitDefinition celsius = CreateUnit("degree_celsius", "degree Celsius", "degrees Celsius", "degC", DimensionVector.ThermodynamicTemperature(), new RationalNumber(1, 1), new RationalNumber(27315, 100), UnitConversionKind.Affine);
        UnitDefinition interval = CreateUnit("kelvin_interval", "kelvin interval", "kelvin intervals", "delta_K", DimensionVector.ThermodynamicTemperature(), new RationalNumber(1, 1), new RationalNumber(0, 1), UnitConversionKind.Scale);

        ConversionPlan unsupportedPlan = ConversionPlan.Create(unsupported, meter);
        ConversionPlan affineMix = ConversionPlan.Create(celsius, interval);

        Assert.That(unsupportedPlan.IsUnsupported(), Is.True);
        Assert.That(unsupportedPlan.GetConversionKind(), Is.EqualTo(UnitConversionKind.Unsupported));
        Assert.That(unsupportedPlan.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionUnsupported));
        Assert.That(unsupportedPlan.GetValueScale().EqualsValue(new RationalNumber(1, 1)), Is.True);
        Assert.That(unsupportedPlan.GetValueOffset().IsZero(), Is.True);
        Assert.That(affineMix.IsUnsupported(), Is.True);
        Assert.That(affineMix.GetConversionKind(), Is.EqualTo(UnitConversionKind.Affine));
        Assert.That(affineMix.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionInvalidAffineOperation));
    }

    [Test]
    public void Create_RejectsMissingUnits()
    {
        UnitDefinition meter = CreateUnit("meter", "meter", "meters", "m", DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), UnitConversionKind.Scale);

        Assert.Throws<System.ArgumentNullException>(() => ConversionPlan.Create(null!, meter));
        Assert.Throws<System.ArgumentNullException>(() => ConversionPlan.Create(meter, null!));
    }

    [Test]
    public void Constructor_ValidatesPlanState()
    {
        UnitDefinition meter = CreateUnit("meter", "meter", "meters", "m", DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), UnitConversionKind.Scale);

        SpecialConversionDefinition specialConversion = new SpecialConversionDefinition("special_meter", SpecialConversionStrategy.Base10LogarithmicRatio, new RationalNumber(1, 1), new RationalNumber(10, 1));

        Assert.Throws<System.ArgumentNullException>(() => new ConversionPlan(null!, meter, UnitConversionKind.Scale, new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitErrorCode.None, "", null));
        Assert.Throws<System.ArgumentNullException>(() => new ConversionPlan(meter, null!, UnitConversionKind.Scale, new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitErrorCode.None, "", null));
        Assert.Throws<System.ArgumentNullException>(() => new ConversionPlan(meter, meter, UnitConversionKind.Scale, null!, new RationalNumber(0, 1), true, UnitErrorCode.None, "", null));
        Assert.Throws<System.ArgumentNullException>(() => new ConversionPlan(meter, meter, UnitConversionKind.Scale, new RationalNumber(1, 1), null!, true, UnitErrorCode.None, "", null));
        Assert.Throws<System.ArgumentException>(() => new ConversionPlan(meter, meter, UnitConversionKind.Scale, new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitErrorCode.ConversionUnsupported, "", null));
        Assert.Throws<System.ArgumentException>(() => new ConversionPlan(meter, meter, UnitConversionKind.Unsupported, new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitErrorCode.None, "", null));
        Assert.Throws<System.ArgumentNullException>(() => new ConversionPlan(meter, meter, UnitConversionKind.Special, new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitErrorCode.None, "", null));
        Assert.Throws<System.ArgumentException>(() => new ConversionPlan(meter, meter, UnitConversionKind.Scale, new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitErrorCode.None, "", specialConversion));
        Assert.Throws<System.ArgumentException>(() => new ConversionPlan(meter, meter, UnitConversionKind.Special, new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitErrorCode.ConversionUnsupported, "", specialConversion));
        Assert.Throws<System.ArgumentException>(() => new ConversionPlan(meter, meter, (UnitConversionKind)999, new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitErrorCode.None, "", null));
        Assert.Throws<System.ArgumentException>(() => new ConversionPlan(meter, meter, UnitConversionKind.Unsupported, new RationalNumber(1, 1), new RationalNumber(0, 1), true, UnitErrorCode.None, "", null));
        Assert.Throws<System.ArgumentException>(() => new ConversionPlan(meter, meter, UnitConversionKind.Scale, new RationalNumber(0, 1), new RationalNumber(0, 1), true, UnitErrorCode.None, "", null));
        Assert.Throws<System.ArgumentException>(() => new ConversionPlan(meter, meter, UnitConversionKind.Scale, new RationalNumber(1, 1), new RationalNumber(1, 1), true, UnitErrorCode.None, "", null));

        ConversionPlan unsupported = new ConversionPlan(meter, meter, UnitConversionKind.Unsupported, new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitErrorCode.ConversionUnsupported, null, null);
        Assert.That(unsupported.GetMessage(), Is.EqualTo(""));
        Assert.That(unsupported.IsUnsupported(), Is.True);
    }

    static UnitDefinition CreateUnit(string id, string name, string pluralName, string symbol, DimensionVector dimension, RationalNumber scale, RationalNumber offset, UnitConversionKind conversionKind)
    {
        return new UnitDefinition(
            id,
            name,
            pluralName,
            symbol,
            new string[0],
            dimension,
            scale,
            offset,
            false,
            conversionKind);
    }
}
