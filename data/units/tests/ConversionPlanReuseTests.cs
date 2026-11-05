using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class ConversionPlanReuseTests
{
    [Test]
    public void ConvertWithPlan_ReusesScaleAndAffinePlansAcrossValues()
    {
        IUnitConverter converter = new UnitConverter();
        UnitDefinition meter = new StandardUnits().Meter();
        UnitDefinition kilometer = new StandardUnits().Kilometer();
        UnitDefinition celsius = new StandardUnits().DegreeCelsius();
        UnitDefinition fahrenheit = new StandardUnits().DegreeFahrenheit();
        ConversionPlan scalePlan = converter.GetPlan(meter, kilometer);
        ConversionPlan affinePlan = converter.GetPlan(celsius, fahrenheit);

        ConversionResult oneKilometer = converter.ConvertWithPlan(1000.0d, scalePlan);
        ConversionResult twoAndHalfKilometers = converter.ConvertWithPlan(2500.0d, scalePlan);
        ConversionResult freezing = converter.ConvertWithPlan(0.0d, affinePlan);
        ConversionResult boiling = converter.ConvertWithPlan(100.0d, affinePlan);

        AssertSuccess(oneKilometer, 1.0d, kilometer);
        AssertSuccess(twoAndHalfKilometers, 2.5d, kilometer);
        AssertSuccess(freezing, 32.0d, fahrenheit);
        AssertSuccess(boiling, 212.0d, fahrenheit);
    }

    [Test]
    public void ConvertWithPlan_ReusesExplicitSpecialPlansInBothDirections()
    {
        IUnitConverter converter = new UnitConverter();
        UnitDefinition decibelMilliwatt = DecibelMilliwatt();
        UnitDefinition watt = new StandardUnits().Watt();
        SpecialConversionDefinition metadata = DecibelMilliwattMetadata();
        ConversionPlan toWatt = converter.GetSpecialPlan(decibelMilliwatt, watt, metadata);
        ConversionPlan toDecibelMilliwatt = converter.GetSpecialPlan(watt, decibelMilliwatt, metadata);

        ConversionResult zeroDbm = converter.ConvertWithPlan(0.0d, toWatt);
        ConversionResult thirtyDbm = converter.ConvertWithPlan(30.0d, toWatt);
        ConversionResult oneMilliwatt = converter.ConvertWithPlan(0.001d, toDecibelMilliwatt);
        ConversionResult oneWatt = converter.ConvertWithPlan(1.0d, toDecibelMilliwatt);

        AssertSuccess(zeroDbm, 0.001d, watt);
        AssertSuccess(thirtyDbm, 1.0d, watt);
        AssertSuccess(oneMilliwatt, 0.0d, decibelMilliwatt);
        AssertSuccess(oneWatt, 30.0d, decibelMilliwatt);
    }

    [Test]
    public void ConvertWithPlan_PreservesCachedFailurePlansAndRejectsMissingPlan()
    {
        IUnitConverter converter = new UnitConverter();
        UnitDefinition meter = new StandardUnits().Meter();
        UnitDefinition unsupportedLength = UnsupportedLength();
        ConversionPlan incompatiblePlan = converter.GetPlan(meter, new StandardUnits().Second());
        ConversionPlan unsupportedPlan = converter.GetPlan(unsupportedLength, meter);
        ConversionPlan invalidSpecialPlan = converter.GetSpecialPlan(DecibelMilliwatt(), meter, DecibelMilliwattMetadata());

        ConversionResult first = converter.ConvertWithPlan(1.0d, incompatiblePlan);
        ConversionResult second = converter.ConvertWithPlan(25.0d, incompatiblePlan);
        ConversionResult unsupported = converter.ConvertWithPlan(1.0d, unsupportedPlan);
        ConversionResult invalidSpecial = converter.ConvertWithPlan(1.0d, invalidSpecialPlan);

        AssertFailure(first, UnitErrorCode.ConversionIncompatibleDimensions, "incompatible");
        AssertFailure(second, UnitErrorCode.ConversionIncompatibleDimensions, "incompatible");
        AssertFailure(unsupported, UnitErrorCode.ConversionUnsupported, "unsupported");
        AssertFailure(invalidSpecial, UnitErrorCode.ConversionIncompatibleDimensions, "compatible dimensions");
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertWithPlan(1.0d, null!));
    }

    static void AssertSuccess(ConversionResult result, double expectedValue, UnitDefinition expectedUnit)
    {
        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetValue(), Is.EqualTo(expectedValue).Within(0.000000001d));
        Assert.That(result.GetUnit(), Is.SameAs(expectedUnit));
    }

    static void AssertFailure(ConversionResult result, UnitErrorCode errorCode, string messageText)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(errorCode));
        Assert.That(result.GetMessage(), Does.Contain(messageText));
        Assert.Throws<System.InvalidOperationException>(() => result.GetValue());
        Assert.Throws<System.InvalidOperationException>(() => result.GetUnit());
    }

    static SpecialConversionDefinition DecibelMilliwattMetadata()
    {
        return new SpecialConversionDefinition("decibel_milliwatt", SpecialConversionStrategy.Base10LogarithmicRatio, new RationalNumber(1, 1000), new RationalNumber(10, 1));
    }

    static UnitDefinition DecibelMilliwatt()
    {
        return new UnitDefinition(
            "decibel_milliwatt",
            "decibel-milliwatt",
            "decibel-milliwatts",
            "dBm",
            new string[0],
            new StandardUnits().Watt().GetDimension(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Special);
    }

    static UnitDefinition UnsupportedLength()
    {
        return new UnitDefinition(
            "unsupported_length",
            "unsupported length",
            "unsupported lengths",
            "ul",
            new string[0],
            DimensionVector.Length(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Unsupported);
    }
}
