using geo;

namespace geo.Tests;

public class RobustArithmeticTests
{
    [Test]
    public void FloatingPointFilterAcceptsSeparatedSignsAndDefersCancellation()
    {
        FloatingPointFilterResult positive = FloatingPointFilter.EvaluateDeterminant2x2(
            3.0, 4.0, -4.0, 3.0);
        FloatingPointFilterResult negative = FloatingPointFilter.EvaluateDeterminant2x2(
            -3.0, 4.0, 4.0, 3.0);
        FloatingPointFilterResult exactZero = FloatingPointFilter.EvaluateDeterminant2x2(
            1.0, 0.0, 2.0, 0.0);
        double baseValue = 134217728.0;
        FloatingPointFilterResult cancellation = FloatingPointFilter.EvaluateDeterminant2x2(
            baseValue + 1.0, baseValue, baseValue + 2.0, baseValue + 1.0);
        FloatingPointFilterResult overflow = FloatingPointFilter.EvaluateDeterminant2x2(
            double.MaxValue, 1.0, 1.0, double.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(positive.Reliable, Is.True);
            Assert.That(positive.Sign, Is.EqualTo(1));
            Assert.That(positive.Estimate, Is.EqualTo(25.0));
            Assert.That(positive.ErrorBound, Is.GreaterThan(0.0));
            Assert.That(negative.Reliable, Is.True);
            Assert.That(negative.Sign, Is.EqualTo(-1));
            Assert.That(exactZero.Reliable, Is.True);
            Assert.That(exactZero.Sign, Is.Zero);
            Assert.That(cancellation.Reliable, Is.False);
            Assert.That(cancellation.Estimate, Is.Zero);
            Assert.That(overflow.Reliable, Is.False);
            Assert.That(double.IsPositiveInfinity(overflow.ErrorBound), Is.True);
        });
    }

    [Test]
    public void ErrorFreeTransformsRetainRoundedAwayLowComponents()
    {
        double[] sum = ExpansionArithmetic.CreateTwoSum(1.0e16, 1.0);
        double[] difference = ExpansionArithmetic.CreateTwoDifference(1.0e16, 1.0);
        double baseValue = 134217728.0;
        double[] product = ExpansionArithmetic.CreateTwoProduct(
            baseValue + 1.0, baseValue + 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(sum, Has.Length.EqualTo(2));
            Assert.That(sum[0], Is.EqualTo(1.0));
            Assert.That(sum[1], Is.EqualTo(1.0e16));
            Assert.That(difference, Has.Length.EqualTo(2));
            Assert.That(difference[0], Is.EqualTo(-1.0));
            Assert.That(difference[1], Is.EqualTo(1.0e16));
            Assert.That(product, Has.Length.EqualTo(2));
            Assert.That(product[0], Is.EqualTo(1.0));
            Assert.That(product[1], Is.EqualTo((baseValue + 1.0) * (baseValue + 1.0)));
        });
    }

    [Test]
    public void ExpansionOperationsPreserveExactCancellationAndSigns()
    {
        double baseValue = 134217728.0;
        double[] determinant = ExpansionArithmetic.CreateProductDifference(
            baseValue + 1.0, baseValue, baseValue + 2.0, baseValue + 1.0);
        double[] reversed = ExpansionArithmetic.CreateProductDifference(
            baseValue, baseValue + 1.0, baseValue + 1.0, baseValue + 2.0);
        double[] retained = ExpansionArithmetic.AddExpansions(
            [1.0, 1.0e16], [-1.0e16]);
        double[] scaled = ExpansionArithmetic.ScaleExpansion([1.0, 1.0e16], 2.0);
        double[] scaledRemainder = ExpansionArithmetic.SubtractExpansions(scaled, [2.0e16]);
        double[] product = ExpansionArithmetic.MultiplyExpansions([1.0, 1.0e8], [1.0, -1.0e8]);

        Assert.Multiple(() =>
        {
            Assert.That(ExpansionArithmetic.GetExpansionSign(determinant), Is.EqualTo(1));
            Assert.That(ExpansionArithmetic.EstimateExpansion(determinant), Is.EqualTo(1.0));
            Assert.That(ExpansionArithmetic.GetExpansionSign(reversed), Is.EqualTo(-1));
            Assert.That(ExpansionArithmetic.EstimateExpansion(reversed), Is.EqualTo(-1.0));
            Assert.That(ExpansionArithmetic.GetExpansionSign(retained), Is.EqualTo(1));
            Assert.That(ExpansionArithmetic.EstimateExpansion(retained), Is.EqualTo(1.0));
            Assert.That(ExpansionArithmetic.EstimateExpansion(scaledRemainder), Is.EqualTo(2.0));
            Assert.That(ExpansionArithmetic.EstimateExpansion(product), Is.EqualTo(-9999999999999999.0));
        });
    }

    [Test]
    public void DoubleDoubleArithmeticRetainsLowWordsAcrossOperations()
    {
        DoubleDouble largePlusOne = new DoubleDouble(1.0e16, 1.0);
        DoubleDouble recovered = largePlusOne.SubtractValue(new DoubleDouble(1.0e16, 0.0));
        double baseValue = 134217728.0;
        DoubleDouble left = new DoubleDouble(baseValue + 1.0, 0.0).MultiplyValue(
            new DoubleDouble(baseValue + 1.0, 0.0));
        DoubleDouble right = new DoubleDouble(baseValue, 0.0).MultiplyValue(
            new DoubleDouble(baseValue + 2.0, 0.0));
        DoubleDouble determinant = left.SubtractValue(right);
        DoubleDouble third = new DoubleDouble(1.0, 0.0).DivideValue(new DoubleDouble(3.0, 0.0));
        DoubleDouble restored = third.MultiplyValue(new DoubleDouble(3.0, 0.0));

        Assert.Multiple(() =>
        {
            Assert.That(largePlusOne.High, Is.EqualTo(1.0e16));
            Assert.That(largePlusOne.Low, Is.EqualTo(1.0));
            Assert.That(recovered.ToDoubleValue(), Is.EqualTo(1.0));
            Assert.That(recovered.CompareToZero(), Is.EqualTo(1));
            Assert.That(determinant.ToDoubleValue(), Is.EqualTo(1.0));
            Assert.That(determinant.CompareToZero(), Is.EqualTo(1));
            Assert.That(determinant.NegateValue().CompareToZero(), Is.EqualTo(-1));
            Assert.That(restored.ToDoubleValue(), Is.EqualTo(1.0).Within(1.0e-30));
            Assert.Throws<InvalidGeometryException>(() =>
                largePlusOne.DivideValue(new DoubleDouble(0.0, 0.0)));
        });
    }

    [Test]
    public void RobustDeterminantResolvesRoundedCancellationExactly()
    {
        double baseValue = 134217728.0;
        int positive = RobustDeterminant.GetSign2x2(
            baseValue + 1.0, baseValue, baseValue + 2.0, baseValue + 1.0);
        int negative = RobustDeterminant.GetSign2x2(
            baseValue, baseValue + 1.0, baseValue + 1.0, baseValue + 2.0);
        int zero = RobustDeterminant.GetSign2x2(
            baseValue + 1.0, baseValue + 1.0, baseValue + 1.0, baseValue + 1.0);
        DoubleDouble value = RobustDeterminant.GetDoubleDoubleValue2x2(
            baseValue + 1.0, baseValue, baseValue + 2.0, baseValue + 1.0);

        Assert.Multiple(() =>
        {
            Assert.That(positive, Is.EqualTo(1));
            Assert.That(negative, Is.EqualTo(-1));
            Assert.That(zero, Is.Zero);
            Assert.That(value.ToDoubleValue(), Is.EqualTo(1.0));
            Assert.That(RobustDeterminant.GetEstimate2x2(
                baseValue + 1.0, baseValue, baseValue + 2.0, baseValue + 1.0), Is.Zero);
        });
    }

    [Test]
    public void ExactFallbackHandlesOverflowUnderflowAndWidelySeparatedExponents()
    {
        double smallest = double.Epsilon;
        double previousMaximum = Math.BitDecrement(double.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(RobustDeterminant.GetSign2x2(
                double.MaxValue, 1.0, 1.0, double.MaxValue), Is.EqualTo(1));
            Assert.That(RobustDeterminant.GetSign2x2(
                1.0, double.MaxValue, double.MaxValue, 1.0), Is.EqualTo(-1));
            Assert.That(RobustDeterminant.GetSign2x2(
                smallest * 2.0, smallest, smallest, smallest), Is.EqualTo(1));
            Assert.That(RobustDeterminant.GetSign2x2(
                smallest, smallest * 2.0, smallest, smallest), Is.EqualTo(-1));
            Assert.That(RobustDeterminant.GetSign2x2(
                double.MaxValue, previousMaximum, smallest, smallest), Is.EqualTo(1));
            Assert.That(RobustDeterminant.GetExactProductDifferenceSign(
                double.MaxValue, previousMaximum, smallest, smallest), Is.EqualTo(1));
        });
    }

    [Test]
    public void ArithmeticRejectsNonFiniteOrUnsupportedExpansionInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidGeometryException>(() =>
                FloatingPointFilter.EvaluateDeterminant2x2(double.NaN, 0.0, 0.0, 1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                ExpansionArithmetic.CreateTwoProduct(double.MaxValue, 2.0));
            Assert.Throws<InvalidGeometryException>(() =>
                ExpansionArithmetic.CreateTwoSum(double.PositiveInfinity, 1.0));
            Assert.Throws<InvalidGeometryException>(() =>
                ExpansionArithmetic.AddExpansions(null!, [1.0]));
            Assert.Throws<InvalidGeometryException>(() =>
                ExpansionArithmetic.GetExpansionSign([]));
            Assert.Throws<InvalidGeometryException>(() => new DoubleDouble(double.NaN, 0.0));
            Assert.Throws<InvalidGeometryException>(() =>
                RobustDeterminant.GetSign2x2(1.0, 2.0, double.NegativeInfinity, 4.0));
        });
    }
}
