using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityMathTests
{
    [Test]
    public void Add_AddsQuantitiesWithSameUnit()
    {
        QuantityValue left = new QuantityValue(2.5d, new StandardUnits().Meter());
        QuantityValue right = new QuantityValue(1.25d, new StandardUnits().Meter());

        QuantityValue result = new QuantityMath().Add(left, right);

        AssertQuantity(result, 3.75d, "meter");
    }

    [Test]
    public void Add_ConvertsRightQuantityToLeftUnit()
    {
        QuantityValue left = new QuantityValue(1.0d, new StandardUnits().Foot());
        QuantityValue right = new QuantityValue(12.0d, new StandardUnits().Inch());

        QuantityValue result = new QuantityMath().Add(left, right);

        AssertQuantity(result, 2.0d, "foot");
    }

    [Test]
    public void Subtract_SubtractsConvertedRightQuantityFromLeftUnit()
    {
        QuantityValue left = new QuantityValue(1.0d, new StandardUnits().Gallon());
        QuantityValue right = new QuantityValue(2.0d, new StandardUnits().Quart());

        QuantityValue result = new QuantityMath().Subtract(left, right);

        AssertQuantity(result, 0.5d, "us_gallon");
    }

    [Test]
    public void AddAndSubtract_PreserveLeftOperandUnitAndDoNotMutateOperands()
    {
        QuantityValue left = new QuantityValue(1000.0d, new StandardUnits().Gram());
        QuantityValue right = new QuantityValue(1.0d, new StandardUnits().Kilogram());

        QuantityValue added = new QuantityMath().Add(left, right);
        QuantityValue subtracted = new QuantityMath().Subtract(left, right);

        AssertQuantity(added, 2000.0d, "gram");
        AssertQuantity(subtracted, 0.0d, "gram");
        AssertQuantity(left, 1000.0d, "gram");
        AssertQuantity(right, 1.0d, "kilogram");
    }

    [Test]
    public void AddAndSubtract_RejectIncompatibleQuantities()
    {
        QuantityValue length = new QuantityValue(1.0d, new StandardUnits().Meter());
        QuantityValue time = new QuantityValue(1.0d, new StandardUnits().Second());

        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Add(length, time));
        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Subtract(length, time));
    }

    [Test]
    public void AddAndSubtract_RejectMissingOperands()
    {
        QuantityValue length = new QuantityValue(1.0d, new StandardUnits().Meter());

        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Add(null!, length));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Add(length, null!));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Subtract(null!, length));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Subtract(length, null!));
    }

    [Test]
    public void Multiply_MultipliesValuesAndDerivesCompoundUnit()
    {
        QuantityValue left = new QuantityValue(3.0d, new StandardUnits().Meter());
        QuantityValue right = new QuantityValue(2.0d, new StandardUnits().Meter());

        QuantityValue result = new QuantityMath().Multiply(left, right);

        AssertQuantity(result, 6.0d, "(meter*meter)");
        Assert.That(result.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2)), Is.True);
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
    }

    [Test]
    public void Multiply_PreservesScaledUnitFactors()
    {
        QuantityValue left = new QuantityValue(2.0d, new StandardUnits().Foot());
        QuantityValue right = new QuantityValue(3.0d, new StandardUnits().Foot());

        QuantityValue result = new QuantityMath().Multiply(left, right);

        AssertQuantity(result, 6.0d, "(foot*foot)");
        Assert.That(result.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2)), Is.True);
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(FootScale().Multiply(FootScale())), Is.True);
    }

    [Test]
    public void Divide_DividesValuesAndDerivesCompoundUnit()
    {
        QuantityValue length = new QuantityValue(10.0d, new StandardUnits().Meter());
        QuantityValue time = new QuantityValue(2.0d, new StandardUnits().Second());

        QuantityValue result = new QuantityMath().Divide(length, time);

        AssertQuantity(result, 5.0d, "(meter/second)");
        Assert.That(result.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time())), Is.True);
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
    }

    [Test]
    public void Divide_PreservesScaledUnitFactors()
    {
        QuantityValue volume = new QuantityValue(2.0d, new StandardUnits().Gallon());
        QuantityValue time = new QuantityValue(4.0d, new StandardUnits().Hour());

        QuantityValue result = new QuantityMath().Divide(volume, time);

        AssertQuantity(result, 0.5d, "(us_gallon/hour)");
        Assert.That(result.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(3).Divide(new StandardDimensions().Time())), Is.True);
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(GallonScale().Divide(new RationalNumber(3600, 1))), Is.True);
    }

    [Test]
    public void MultiplyAndDivide_RejectMissingOperands()
    {
        QuantityValue length = new QuantityValue(1.0d, new StandardUnits().Meter());

        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Multiply(null!, length));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Multiply(length, null!));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Divide(null!, length));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Divide(length, null!));
    }

    [Test]
    public void MultiplyAndDivide_RejectAffineUnits()
    {
        QuantityValue temperature = new QuantityValue(1.0d, new StandardUnits().Kelvin());
        QuantityValue length = new QuantityValue(1.0d, new StandardUnits().Meter());

        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Multiply(temperature, length));
        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Divide(temperature, length));
    }

    [Test]
    public void ScaleNegateAndAbsoluteValue_AdjustValueAndPreserveUnit()
    {
        QuantityValue value = new QuantityValue(-3.0d, new StandardUnits().Meter());

        QuantityValue scaled = new QuantityMath().Scale(value, 2.5d);
        QuantityValue negated = new QuantityMath().Negate(value);
        QuantityValue absolute = new QuantityMath().AbsoluteValue(value);
        QuantityValue abs = new QuantityMath().Abs(value);

        AssertQuantity(scaled, -7.5d, "meter");
        AssertQuantity(negated, 3.0d, "meter");
        AssertQuantity(absolute, 3.0d, "meter");
        AssertQuantity(abs, 3.0d, "meter");
        AssertQuantity(value, -3.0d, "meter");
    }

    [Test]
    public void Pow_RaisesValueAndUnitToIntegerPower()
    {
        QuantityValue value = new QuantityValue(3.0d, new StandardUnits().Foot());

        QuantityValue result = new QuantityMath().Pow(value, 2);

        AssertQuantity(result, 9.0d, "(foot^2)");
        Assert.That(result.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2)), Is.True);
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(FootScale().Multiply(FootScale())), Is.True);
    }

    [Test]
    public void Pow_WithNegativePowerCreatesInverseQuantity()
    {
        QuantityValue value = new QuantityValue(2.0d, new StandardUnits().Second());

        QuantityValue result = new QuantityMath().Pow(value, -1);

        AssertQuantity(result, 0.5d, "(second^-1)");
        Assert.That(result.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Time().Pow(-1)), Is.True);
        Assert.That(result.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
    }

    [Test]
    public void Root_ReturnsRootedQuantityWhenValueAndUnitAreRootable()
    {
        QuantityValue area = new QuantityValue(9.0d, new StandardUnits().Foot().Pow(2));

        QuantityRootResult result = new QuantityMath().Root(area, 2);

        Assert.That(result.IsSuccess(), Is.True);
        AssertQuantity(result.GetQuantity(), 3.0d, "((foot^2)^1/2)");
        Assert.That(result.GetQuantity().GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length()), Is.True);
        Assert.That(result.GetQuantity().GetUnit().GetScaleToSystemUnit().EqualsValue(FootScale()), Is.True);
    }

    [Test]
    public void Root_ReturnsSignedRootForNegativeOddRoots()
    {
        QuantityValue volume = new QuantityValue(-8.0d, new StandardUnits().Meter().Pow(3));

        QuantityRootResult result = new QuantityMath().Root(volume, 3);

        Assert.That(result.IsSuccess(), Is.True);
        AssertQuantity(result.GetQuantity(), -2.0d, "((meter^3)^1/3)");
        Assert.That(result.GetQuantity().GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length()), Is.True);
    }

    [Test]
    public void Root_ReturnsFailureForInvalidRootOrUnrootableQuantity()
    {
        QuantityValue length = new QuantityValue(4.0d, new StandardUnits().Meter());
        QuantityValue negativeArea = new QuantityValue(-4.0d, new StandardUnits().Meter().Pow(2));

        QuantityRootResult zeroRoot = new QuantityMath().Root(length, 0);
        QuantityRootResult unrootableUnit = new QuantityMath().Root(length, 2);
        QuantityRootResult negativeEvenRoot = new QuantityMath().Root(negativeArea, 2);

        Assert.That(zeroRoot.IsFailure(), Is.True);
        Assert.That(zeroRoot.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
        Assert.That(unrootableUnit.IsFailure(), Is.True);
        Assert.That(unrootableUnit.GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
        Assert.That(unrootableUnit._quantity, Is.Null);
        Assert.That(negativeEvenRoot.IsFailure(), Is.True);
        Assert.That(negativeEvenRoot.GetMessage(), Does.Contain("negative"));
        Assert.Throws<System.InvalidOperationException>(() => unrootableUnit.GetQuantity());
    }

    [Test]
    public void ScalarPowerAndRootOperations_RejectMissingQuantity()
    {
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Scale(null!, 2.0d));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Negate(null!));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().AbsoluteValue(null!));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Abs(null!));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Pow(null!, 2));
        Assert.Throws<System.ArgumentNullException>(() => new QuantityMath().Root(null!, 2));
    }

    [Test]
    public void QuantityRootResult_ValidatesSuccessAndFailureStates()
    {
        QuantityValue value = new QuantityValue(1.0d, new StandardUnits().Meter());

        Assert.Throws<System.ArgumentNullException>(() => QuantityRootResult.CreateSuccess(null!));
        Assert.Throws<System.ArgumentException>(() => new QuantityRootResult(true, value, UnitErrorCode.ArithmeticInvalidOperation, ""));
        Assert.Throws<System.ArgumentException>(() => new QuantityRootResult(false, value, UnitErrorCode.None, ""));
        Assert.Throws<System.ArgumentException>(() => new QuantityRootResult(false, value, UnitErrorCode.ArithmeticInvalidOperation, ""));
        Assert.Throws<System.ArgumentException>(() => new QuantityRootResult(false, null, UnitErrorCode.None, ""));

        QuantityRootResult failure = QuantityRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, null);
        Assert.That(failure.GetMessage(), Is.EqualTo(""));
        Assert.Throws<System.InvalidOperationException>(() => failure.GetQuantity());
    }

    [Test]
    public void QuantityArithmetic_ComposesMechanicsDimensions()
    {
        QuantityValue distance = new QuantityValue(20.0d, new StandardUnits().Meter());
        QuantityValue time = new QuantityValue(4.0d, new StandardUnits().Second());
        QuantityValue mass = new QuantityValue(3.0d, new StandardUnits().Kilogram());

        QuantityValue speed = new QuantityMath().Divide(distance, time);
        QuantityValue acceleration = new QuantityMath().Divide(speed, time);
        QuantityValue force = new QuantityMath().Multiply(mass, acceleration);
        QuantityValue energy = new QuantityMath().Multiply(force, distance);

        AssertQuantity(speed, 5.0d, "(meter/second)");
        Assert.That(speed.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time())), Is.True);
        AssertQuantity(acceleration, 1.25d, "((meter/second)/second)");
        Assert.That(acceleration.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Divide(new StandardDimensions().Time().Pow(2))), Is.True);
        AssertQuantity(force, 3.75d, "(kilogram*((meter/second)/second))");
        Assert.That(force.GetUnit().GetDimension().EqualsDimension(new StandardUnits().Newton().GetDimension()), Is.True);
        AssertQuantity(energy, 75.0d, "((kilogram*((meter/second)/second))*meter)");
        Assert.That(energy.GetUnit().GetDimension().EqualsDimension(new StandardUnits().Joule().GetDimension()), Is.True);
    }

    [Test]
    public void QuantityArithmetic_DividingCompatibleQuantitiesCreatesDimensionlessRatio()
    {
        QuantityValue numerator = new QuantityValue(10.0d, new StandardUnits().Meter());
        QuantityValue denominator = new QuantityValue(2.0d, new StandardUnits().Meter());

        QuantityValue ratio = new QuantityMath().Divide(numerator, denominator);

        AssertQuantity(ratio, 5.0d, "(meter/meter)");
        Assert.That(ratio.IsDimensionless(), Is.True);
        Assert.That(ratio.GetUnit().GetScaleToSystemUnit().EqualsValue(new RationalNumber(1, 1)), Is.True);
    }

    [Test]
    public void QuantityArithmetic_RootsMultipliedAreaBackToLengthDimension()
    {
        QuantityValue width = new QuantityValue(3.0d, new StandardUnits().Meter());
        QuantityValue height = new QuantityValue(12.0d, new StandardUnits().Meter());
        QuantityValue area = new QuantityMath().Multiply(width, height);

        QuantityRootResult root = new QuantityMath().Root(area, 2);

        Assert.That(area.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length().Pow(2)), Is.True);
        Assert.That(root.IsSuccess(), Is.True);
        Assert.That(root.GetQuantity().GetUnit().GetDimension().EqualsDimension(new StandardDimensions().Length()), Is.True);
        Assert.That(root.GetQuantity().GetValue(), Is.EqualTo(6.0d).Within(0.000000000001d));
    }

    [Test]
    public void QuantityArithmetic_RejectsAffineTemperaturePointArithmetic()
    {
        QuantityValue temperature = new QuantityValue(300.0d, new StandardUnits().Kelvin());
        QuantityValue otherTemperature = new QuantityValue(10.0d, new StandardUnits().Kelvin());

        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Add(temperature, otherTemperature));
        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Subtract(temperature, otherTemperature));
        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Multiply(temperature, otherTemperature));
        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Divide(temperature, otherTemperature));
        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Scale(temperature, 2.0d));
        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Negate(temperature));
        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().AbsoluteValue(temperature));
        Assert.Throws<System.InvalidOperationException>(() => new QuantityMath().Pow(temperature, 2));
        Assert.That(new QuantityMath().Root(temperature, 2).IsFailure(), Is.True);
        Assert.That(new QuantityMath().Root(temperature, 2).GetErrorCode(), Is.EqualTo(UnitErrorCode.ArithmeticInvalidOperation));
    }

    [Test]
    public void QuantityArithmetic_AllowsScaleOnlyTemperatureIntervals()
    {
        QuantityValue interval = new QuantityValue(4.0d, KelvinInterval());
        QuantityValue otherInterval = new QuantityValue(2.0d, KelvinInterval());

        QuantityValue added = new QuantityMath().Add(interval, otherInterval);
        QuantityValue subtracted = new QuantityMath().Subtract(interval, otherInterval);
        QuantityValue scaled = new QuantityMath().Scale(interval, 3.0d);
        QuantityValue negated = new QuantityMath().Negate(interval);
        QuantityValue absolute = new QuantityMath().AbsoluteValue(negated);
        QuantityValue squared = new QuantityMath().Pow(otherInterval, 2);
        QuantityRootResult rooted = new QuantityMath().Root(squared, 2);

        AssertQuantity(added, 6.0d, "kelvin_interval");
        AssertQuantity(subtracted, 2.0d, "kelvin_interval");
        AssertQuantity(scaled, 12.0d, "kelvin_interval");
        AssertQuantity(negated, -4.0d, "kelvin_interval");
        AssertQuantity(absolute, 4.0d, "kelvin_interval");
        Assert.That(squared.GetUnit().GetDimension().EqualsDimension(new StandardDimensions().ThermodynamicTemperature().Pow(2)), Is.True);
        Assert.That(rooted.IsSuccess(), Is.True);
        AssertQuantity(rooted.GetQuantity(), 2.0d, "((kelvin_interval^2)^1/2)");
    }

    static void AssertQuantity(QuantityValue quantity, double expectedValue, string expectedUnitId)
    {
        Assert.That(quantity.GetValue(), Is.EqualTo(expectedValue).Within(0.000000000001d));
        Assert.That(quantity.GetUnit().GetId(), Is.EqualTo(expectedUnitId));
    }

    static RationalNumber FootScale()
    {
        return new RationalNumber(381, 1250);
    }

    static RationalNumber GallonScale()
    {
        return new RationalNumber(473176473, 125000000000);
    }

    static UnitDefinition KelvinInterval()
    {
        return new UnitDefinition(
            "kelvin_interval",
            "kelvin interval",
            "kelvin intervals",
            "delta_K",
            new string[0],
            DimensionVector.ThermodynamicTemperature(),
            new RationalNumber(1, 1),
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
    }
}
