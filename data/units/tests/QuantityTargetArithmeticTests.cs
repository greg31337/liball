using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityTargetArithmeticTests
{
    [Test]
    public void AddToUnit_AddsScaleQuantitiesInExplicitTargetUnit()
    {
        IQuantityMath math = new QuantityMath();
        QuantityValue left = new QuantityValue(1.0d, new StandardUnits().Foot());
        QuantityValue right = new QuantityValue(12.0d, new StandardUnits().Inch());

        QuantityArithmeticResult result = math.AddToUnit(left, right, new StandardUnits().Meter());

        AssertSuccess(result, 0.6096d, "meter");
        AssertQuantity(left, 1.0d, "foot");
        AssertQuantity(right, 12.0d, "inch");
    }

    [Test]
    public void SubtractToUnit_SubtractsScaleQuantitiesInExplicitTargetUnit()
    {
        QuantityMath math = new QuantityMath();
        QuantityValue left = new QuantityValue(1.0d, new StandardUnits().Gallon());
        QuantityValue right = new QuantityValue(2.0d, new StandardUnits().Quart());

        QuantityArithmeticResult result = math.SubtractToUnit(left, right, new StandardUnits().Quart());

        AssertSuccess(result, 2.0d, "us_quart");
    }

    [Test]
    public void AddToUnit_AddsPointAndIntervalInEitherOperandOrder()
    {
        QuantityMath math = new QuantityMath();
        QuantityValue point = new QuantityValue(20.0d, new StandardUnits().DegreeCelsius());
        QuantityValue interval = new QuantityValue(18.0d, new StandardUnits().FahrenheitInterval());

        QuantityArithmeticResult pointFirst = math.AddToUnit(point, interval, new StandardUnits().DegreeFahrenheit());
        QuantityArithmeticResult intervalFirst = math.AddToUnit(interval, point, new StandardUnits().DegreeCelsius());

        AssertSuccess(pointFirst, 86.0d, "degree_fahrenheit");
        AssertSuccess(intervalFirst, 30.0d, "degree_celsius");
    }

    [Test]
    public void SubtractToUnit_SubtractsIntervalFromPointAndPointsToInterval()
    {
        QuantityMath math = new QuantityMath();
        QuantityValue celsiusTwenty = new QuantityValue(20.0d, new StandardUnits().DegreeCelsius());
        QuantityValue fahrenheitInterval = new QuantityValue(18.0d, new StandardUnits().FahrenheitInterval());
        QuantityValue celsiusThirty = new QuantityValue(30.0d, new StandardUnits().DegreeCelsius());
        QuantityValue fahrenheitSixtyEight = new QuantityValue(68.0d, new StandardUnits().DegreeFahrenheit());

        QuantityArithmeticResult point = math.SubtractToUnit(celsiusTwenty, fahrenheitInterval, new StandardUnits().DegreeFahrenheit());
        QuantityArithmeticResult interval = math.SubtractToUnit(celsiusThirty, fahrenheitSixtyEight, new StandardUnits().KelvinInterval());

        AssertSuccess(point, 50.0d, "degree_fahrenheit");
        AssertSuccess(interval, 10.0d, "kelvin_interval");
    }

    [Test]
    public void TargetArithmetic_HandlesScaleOnlyTemperatureIntervals()
    {
        QuantityMath math = new QuantityMath();
        QuantityValue celsiusInterval = new QuantityValue(10.0d, new StandardUnits().CelsiusInterval());
        QuantityValue fahrenheitInterval = new QuantityValue(18.0d, new StandardUnits().FahrenheitInterval());

        QuantityArithmeticResult sum = math.AddToUnit(celsiusInterval, fahrenheitInterval, new StandardUnits().FahrenheitInterval());
        QuantityArithmeticResult difference = math.SubtractToUnit(fahrenheitInterval, celsiusInterval, new StandardUnits().KelvinInterval());

        AssertSuccess(sum, 36.0d, "fahrenheit_interval");
        AssertSuccess(difference, 0.0d, "kelvin_interval");
    }

    [Test]
    public void TargetArithmetic_ReturnsTypedDimensionFailures()
    {
        QuantityMath math = new QuantityMath();
        QuantityValue length = new QuantityValue(1.0d, new StandardUnits().Meter());
        QuantityValue time = new QuantityValue(1.0d, new StandardUnits().Second());
        QuantityValue otherLength = new QuantityValue(1.0d, new StandardUnits().Foot());

        QuantityArithmeticResult incompatibleOperands = math.AddToUnit(length, time, new StandardUnits().Meter());
        QuantityArithmeticResult incompatibleTarget = math.SubtractToUnit(length, otherLength, new StandardUnits().Second());

        AssertFailure(incompatibleOperands, UnitErrorCode.ArithmeticIncompatibleDimensions, "compatible dimensions");
        AssertFailure(incompatibleTarget, UnitErrorCode.ArithmeticIncompatibleDimensions, "compatible dimensions");
    }

    [Test]
    public void AddToUnit_ReturnsTypedFailuresForInvalidAffineCombinations()
    {
        QuantityMath math = new QuantityMath();
        QuantityValue point = new QuantityValue(20.0d, new StandardUnits().DegreeCelsius());
        QuantityValue otherPoint = new QuantityValue(68.0d, new StandardUnits().DegreeFahrenheit());
        QuantityValue interval = new QuantityValue(10.0d, new StandardUnits().CelsiusInterval());

        QuantityArithmeticResult twoPoints = math.AddToUnit(point, otherPoint, new StandardUnits().DegreeCelsius());
        QuantityArithmeticResult pointToInterval = math.AddToUnit(point, interval, new StandardUnits().KelvinInterval());
        QuantityArithmeticResult intervalsToPoint = math.AddToUnit(interval, interval, new StandardUnits().Kelvin());

        AssertFailure(twoPoints, UnitErrorCode.ArithmeticInvalidOperation, "two affine");
        AssertFailure(pointToInterval, UnitErrorCode.ArithmeticInvalidOperation, "target unit");
        AssertFailure(intervalsToPoint, UnitErrorCode.ArithmeticInvalidOperation, "target unit");
    }

    [Test]
    public void SubtractToUnit_ReturnsTypedFailuresForInvalidAffineCombinations()
    {
        QuantityMath math = new QuantityMath();
        QuantityValue point = new QuantityValue(20.0d, new StandardUnits().DegreeCelsius());
        QuantityValue otherPoint = new QuantityValue(68.0d, new StandardUnits().DegreeFahrenheit());
        QuantityValue interval = new QuantityValue(10.0d, new StandardUnits().CelsiusInterval());

        QuantityArithmeticResult pointFromInterval = math.SubtractToUnit(interval, point, new StandardUnits().KelvinInterval());
        QuantityArithmeticResult pointDifferenceToPoint = math.SubtractToUnit(point, otherPoint, new StandardUnits().Kelvin());
        QuantityArithmeticResult pointMinusIntervalToInterval = math.SubtractToUnit(point, interval, new StandardUnits().KelvinInterval());

        AssertFailure(pointFromInterval, UnitErrorCode.ArithmeticInvalidOperation, "cannot be subtracted");
        AssertFailure(pointDifferenceToPoint, UnitErrorCode.ArithmeticInvalidOperation, "target unit");
        AssertFailure(pointMinusIntervalToInterval, UnitErrorCode.ArithmeticInvalidOperation, "target unit");
    }

    [Test]
    public void TargetArithmetic_ReturnsTypedFailuresForUnsupportedConversionKinds()
    {
        QuantityMath math = new QuantityMath();
        UnitDefinition specialLength = new UnitDefinition("special_length", "special length", "special lengths", "spl", new string[0], DimensionVector.Length(), new RationalNumber(1, 1), new RationalNumber(0, 1), false, UnitConversionKind.Special);
        QuantityValue special = new QuantityValue(1.0d, specialLength);
        QuantityValue meter = new QuantityValue(1.0d, new StandardUnits().Meter());

        QuantityArithmeticResult specialOperand = math.AddToUnit(special, meter, new StandardUnits().Meter());
        QuantityArithmeticResult specialTarget = math.SubtractToUnit(meter, meter, specialLength);

        AssertFailure(specialOperand, UnitErrorCode.ArithmeticInvalidOperation, "scale or affine");
        AssertFailure(specialTarget, UnitErrorCode.ArithmeticInvalidOperation, "scale or affine");
    }

    [Test]
    public void TargetArithmetic_RejectsMissingInputs()
    {
        QuantityMath math = new QuantityMath();
        QuantityValue meter = new QuantityValue(1.0d, new StandardUnits().Meter());
        UnitDefinition target = new StandardUnits().Meter();

        Assert.Throws<System.ArgumentNullException>(() => math.AddToUnit(null!, meter, target));
        Assert.Throws<System.ArgumentNullException>(() => math.AddToUnit(meter, null!, target));
        Assert.Throws<System.ArgumentNullException>(() => math.AddToUnit(meter, meter, null!));
        Assert.Throws<System.ArgumentNullException>(() => math.SubtractToUnit(null!, meter, target));
        Assert.Throws<System.ArgumentNullException>(() => math.SubtractToUnit(meter, null!, target));
        Assert.Throws<System.ArgumentNullException>(() => math.SubtractToUnit(meter, meter, null!));
    }

    static void AssertSuccess(QuantityArithmeticResult result, double expectedValue, string expectedUnitId)
    {
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(UnitErrorCode.None));
        AssertQuantity(result.GetQuantity(), expectedValue, expectedUnitId);
    }

    static void AssertFailure(QuantityArithmeticResult result, UnitErrorCode errorCode, string messageText)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetErrorCode(), Is.EqualTo(errorCode));
        Assert.That(result.GetMessage(), Does.Contain(messageText));
        Assert.Throws<System.InvalidOperationException>(() => result.GetQuantity());
    }

    static void AssertQuantity(QuantityValue quantity, double expectedValue, string expectedUnitId)
    {
        Assert.That(quantity.GetValue(), Is.EqualTo(expectedValue).Within(0.000000000001d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }
}
