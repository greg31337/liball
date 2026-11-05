using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitSystemConverterTests
{
    [Test]
    public void ConvertToPreferredUnit_ConvertsLengthToSiPreferredUnit()
    {
        UnitSystemConverter converter = new UnitSystemConverter();

        ConversionResult result = converter.ConvertToPreferredUnit(
            12.0d,
            new StandardUnits().Inch(),
            new StandardQuantityKinds().Length(),
            new StandardUnitSystems().Si(),
            CreateStandardUnitRegistry());

        AssertConversion(result, 0.3048d, "meter", 0.000000000001d);
    }

    [Test]
    public void ConvertToPreferredUnit_ConvertsVolumeToUsCustomaryPreferredUnit()
    {
        UnitSystemConverter converter = new UnitSystemConverter();

        ConversionResult result = converter.ConvertToPreferredUnit(
            2.0d,
            new StandardUnits().Liter(),
            new StandardQuantityKinds().Volume(),
            new StandardUnitSystems().UsCustomary(),
            CreateStandardUnitRegistry());

        AssertConversion(result, 0.5283441047162968d, "us_gallon", 0.000000000001d);
    }

    [Test]
    public void ConvertToPreferredUnit_ConvertsPowerToSiPreferredUnit()
    {
        UnitSystemConverter converter = new UnitSystemConverter();

        ConversionResult result = converter.ConvertToPreferredUnit(
            1.0d,
            new StandardUnits().Horsepower(),
            new StandardQuantityKinds().Power(),
            new StandardUnitSystems().Si(),
            CreateStandardUnitRegistry());

        AssertConversion(result, 745.6998715822702d, "watt", 0.000000001d);
    }

    [Test]
    public void ConvertToPreferredUnit_ConvertsDataSizeToSiPreferredUnit()
    {
        UnitSystemConverter converter = new UnitSystemConverter();

        ConversionResult result = converter.ConvertToPreferredUnit(
            1.0d,
            new StandardUnits().Mebibyte(),
            new StandardQuantityKinds().DataSize(),
            new StandardUnitSystems().Si(),
            CreateStandardUnitRegistry());

        AssertConversion(result, 1048576.0d, "byte", 0.000000000001d);
    }

    [Test]
    public void ConvertToPreferredUnit_ReturnsLookupFailureWhenPreferredUnitIsMissing()
    {
        UnitSystemConverter converter = new UnitSystemConverter();

        ConversionResult result = converter.ConvertToPreferredUnit(
            1.0d,
            new StandardUnits().Meter(),
            new StandardQuantityKinds().Length(),
            new StandardUnitSystems().Cgs(),
            CreateStandardUnitRegistry());

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.That(result.GetMessage(), Does.Contain("centimeter"));
    }

    [Test]
    public void ConvertToPreferredUnit_ReturnsLookupFailureWhenSystemHasNoPreference()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        UnitSystemDefinition system = new UnitSystemDefinition("empty", "Empty", new string[0], new string[0], new string[0]);

        ConversionResult result = converter.ConvertToPreferredUnit(
            1.0d,
            new StandardUnits().Meter(),
            new StandardQuantityKinds().Length(),
            system,
            CreateStandardUnitRegistry());

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.LookupNotFound));
        Assert.That(result.GetMessage(), Does.Contain("length"));
    }

    [Test]
    public void ConvertToPreferredUnit_RejectsQuantityKindDimensionMismatch()
    {
        UnitSystemConverter converter = new UnitSystemConverter();

        ConversionResult result = converter.ConvertToPreferredUnit(
            1.0d,
            new StandardUnits().Second(),
            new StandardQuantityKinds().Length(),
            new StandardUnitSystems().Si(),
            CreateStandardUnitRegistry());

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.ConversionIncompatibleDimensions));
    }

    [Test]
    public void ConvertToPreferredUnit_RejectsMissingInputs()
    {
        UnitSystemConverter converter = new UnitSystemConverter();
        UnitRegistry registry = CreateStandardUnitRegistry();

        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertToPreferredUnit(1.0d, null!, new StandardQuantityKinds().Length(), new StandardUnitSystems().Si(), registry));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertToPreferredUnit(1.0d, new StandardUnits().Meter(), null!, new StandardUnitSystems().Si(), registry));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertToPreferredUnit(1.0d, new StandardUnits().Meter(), new StandardQuantityKinds().Length(), null!, registry));
        Assert.Throws<System.ArgumentNullException>(() => converter.ConvertToPreferredUnit(1.0d, new StandardUnits().Meter(), new StandardQuantityKinds().Length(), new StandardUnitSystems().Si(), null!));
    }

    static void AssertConversion(ConversionResult result, double expectedValue, string expectedUnitId, double tolerance)
    {
        Assert.That(result.IsSuccess(), Is.True, result.GetMessage());
        Assert.That(result.GetValue(), Is.EqualTo(expectedValue).Within(tolerance));
        Assert.That(result.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static UnitRegistry CreateStandardUnitRegistry()
    {
        UnitRegistry registry = new UnitRegistry();

        new StandardUnits().RegisterSiBaseUnits(registry);
        new StandardUnits().RegisterSiDerivedUnits(registry);
        new StandardUnits().RegisterMetricUnits(registry);
        new StandardUnits().RegisterImperialAndUsCustomaryUnits(registry);
        new StandardUnits().RegisterDataUnits(registry);
        return registry;
    }
}
