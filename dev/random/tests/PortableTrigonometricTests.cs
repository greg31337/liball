namespace random.Tests;

public class PortableTrigonometricTests
{
    const double Pi = 3.14159265358979323846;

    [Test]
    public void ForwardKernelsLockPortableBitFixtures()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableMath.Sin(0.0)), Is.EqualTo(0UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Sin(-0.0)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(PortableMath.DoubleBits(PortableMath.Cos(0.0)), Is.EqualTo(0x3FF0000000000000UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Sin(Pi / 6.0)), Is.EqualTo(0x3FDFFFFFFFFFFFFFUL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Cos(Pi / 3.0)), Is.EqualTo(0x3FE0000000000001UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Tan(Pi / 4.0)), Is.EqualTo(0x3FEFFFFFFFFFFFFFUL));
        });
    }

    [Test]
    public void InverseKernelsLockPortableBitFixtures()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableMath.Atan(1.0)), Is.EqualTo(0x3FE921FB54442D18UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Atan2(1.0, -1.0)), Is.EqualTo(0x4002D97C7F3321D2UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Asin(0.5)), Is.EqualTo(0x3FE0C152382D7366UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Acos(0.5)), Is.EqualTo(0x3FF0C152382D7365UL));
        });
    }

    [Test]
    public void QuadrantReductionTracksReferenceAcrossModerateArguments()
    {
        double[] values = [-1000000.0, -12345.6789, -100.0, -2.0 * Pi, -1.5 * Pi, -Pi, -Pi / 2.0, -0.1, 0.1, Pi / 2.0, Pi, 1.5 * Pi, 2.0 * Pi, 100.0, 12345.6789, 1000000.0];

        foreach (double value in values)
        {
            AssertAbsolute(PortableMath.Sin(value), Math.Sin(value), PortableMath.TrigAbsoluteError);
            AssertAbsolute(PortableMath.Cos(value), Math.Cos(value), PortableMath.TrigAbsoluteError);
        }
    }

    [Test]
    public void PeriodicOffsetsUseConsistentQuadrants()
    {
        double baseAngle = 0.321987654321;
        for (int turns = -1000; turns <= 1000; turns += 125)
        {
            double shifted = baseAngle + turns * 2.0 * Pi;
            AssertAbsolute(PortableMath.Sin(shifted), Math.Sin(shifted), PortableMath.TrigAbsoluteError);
            AssertAbsolute(PortableMath.Cos(shifted), Math.Cos(shifted), PortableMath.TrigAbsoluteError);
        }
    }

    [Test]
    public void HugeFiniteFallbackIsDeterministicAndBounded()
    {
        double[] values = [1.0e20, -1.0e100, double.MaxValue];

        foreach (double value in values)
        {
            double firstSin = PortableMath.Sin(value);
            double secondSin = PortableMath.Sin(value);
            double firstCos = PortableMath.Cos(value);
            double secondCos = PortableMath.Cos(value);
            Assert.That(firstSin, Is.EqualTo(secondSin));
            Assert.That(firstCos, Is.EqualTo(secondCos));
            Assert.That(Math.Abs(firstSin), Is.LessThanOrEqualTo(1.0));
            Assert.That(Math.Abs(firstCos), Is.LessThanOrEqualTo(1.0));
        }
    }

    [Test]
    public void AtanAndAtan2CoverSignsZerosAndInfinities()
    {
        double negativeZero = PortableMath.DoubleFromBits(PortableMath.DoubleSignMask);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableMath.Atan(negativeZero)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(PortableMath.Atan(double.PositiveInfinity), Is.EqualTo(Pi / 2.0));
            Assert.That(PortableMath.Atan(double.NegativeInfinity), Is.EqualTo(-Pi / 2.0));
            Assert.That(PortableMath.DoubleBits(PortableMath.Atan2(negativeZero, 1.0)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(PortableMath.Atan2(0.0, -1.0), Is.EqualTo(Pi));
            Assert.That(PortableMath.Atan2(negativeZero, -1.0), Is.EqualTo(-Pi));
            Assert.That(PortableMath.Atan2(double.PositiveInfinity, double.PositiveInfinity), Is.EqualTo(Pi / 4.0));
            Assert.That(PortableMath.Atan2(double.NegativeInfinity, double.NegativeInfinity), Is.EqualTo(-3.0 * Pi / 4.0));
        });
    }

    [Test]
    public void InverseKernelsTrackReferenceAcrossDomains()
    {
        double[] unitValues = [-1.0, -0.999999, -0.75, -0.25, 0.0, 0.25, 0.75, 0.999999, 1.0];
        foreach (double value in unitValues)
        {
            AssertAbsolute(PortableMath.Asin(value), Math.Asin(value), PortableMath.InverseTrigAbsoluteError);
            AssertAbsolute(PortableMath.Acos(value), Math.Acos(value), PortableMath.InverseTrigAbsoluteError);
        }

        double[] atanValues = [-1.0e100, -10.0, -1.0, -0.25, 0.25, 1.0, 10.0, 1.0e100];
        foreach (double value in atanValues)
        {
            AssertAbsolute(PortableMath.Atan(value), Math.Atan(value), PortableMath.InverseTrigAbsoluteError);
        }
    }

    [Test]
    public void InvalidDomainsAndNonFiniteAnglesReturnCanonicalNaN()
    {
        double unusualNaN = PortableMath.DoubleFromBits(0xFFF0000000000001UL);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableMath.Sin(double.PositiveInfinity)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.DoubleBits(PortableMath.Cos(unusualNaN)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.DoubleBits(PortableMath.Tan(double.NegativeInfinity)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.DoubleBits(PortableMath.Asin(1.0001)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.DoubleBits(PortableMath.Acos(-1.0001)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.DoubleBits(PortableMath.Atan2(unusualNaN, 1.0)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
        });
    }

    static void AssertAbsolute(double actual, double expected, double tolerance)
    {
        Assert.That(Math.Abs(actual - expected), Is.LessThanOrEqualTo(tolerance), $"actual={actual:R}, expected={expected:R}");
    }
}
