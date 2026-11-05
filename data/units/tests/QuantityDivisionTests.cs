using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class QuantityDivisionTests
{
    [Test]
    public void Divide_RejectsPositiveAndNegativeZeroDivisors()
    {
        IQuantityMath math = new QuantityMath();
        QuantityValue distance = new QuantityValue(10.0d, new StandardUnits().Meter());
        QuantityValue positiveZero = new QuantityValue(0.0d, new StandardUnits().Second());
        QuantityValue negativeZero = new QuantityValue(-0.0d, new StandardUnits().Second());

        System.DivideByZeroException positive = Assert.Throws<System.DivideByZeroException>(() => math.Divide(distance, positiveZero))!;
        System.DivideByZeroException negative = Assert.Throws<System.DivideByZeroException>(() => math.Divide(distance, negativeZero))!;

        Assert.That(positive.Message, Does.Contain("zero numeric value"));
        Assert.That(negative.Message, Does.Contain("zero numeric value"));
        Assert.That(distance.GetValue(), Is.EqualTo(10.0d));
        Assert.That(positiveZero.GetValue(), Is.EqualTo(0.0d));
    }

    [Test]
    public void Pow_RejectsZeroWithNegativePowerButSupportsZeroAndPositivePowers()
    {
        IQuantityMath math = new QuantityMath();
        QuantityValue zero = new QuantityValue(0.0d, new StandardUnits().Meter());

        QuantityValue identity = math.Pow(zero, 0);
        QuantityValue squared = math.Pow(zero, 2);
        System.DivideByZeroException negative = Assert.Throws<System.DivideByZeroException>(() => math.Pow(zero, -1))!;

        Assert.That(identity.GetValue(), Is.EqualTo(1.0d));
        Assert.That(identity.GetUnit().GetDimension().IsDimensionless(), Is.True);
        Assert.That(squared.GetValue(), Is.EqualTo(0.0d));
        Assert.That(squared.GetUnit().GetDimension().EqualsDimension(DimensionVector.Length().Pow(2)), Is.True);
        Assert.That(negative.Message, Does.Contain("negative power"));
    }
}
