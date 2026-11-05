namespace random.Tests;

public class PortableMathTests
{
    [Test]
    public void DoubleBitPartsRoundTripExactPatterns()
    {
        double value = PortableMath.ComposeDouble(true, 0x3FE, 0x000ABCDEF123456UL);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(value), Is.EqualTo(0xBFE0ABCDEF123456UL));
            Assert.That(PortableMath.DoubleSignBit(value), Is.True);
            Assert.That(PortableMath.DoubleExponentBits(value), Is.EqualTo(0x3FE));
            Assert.That(PortableMath.DoubleFractionBits(value), Is.EqualTo(0x000ABCDEF123456UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.DoubleFromBits(0x8000000000000000UL)), Is.EqualTo(0x8000000000000000UL));
        });
    }

    [Test]
    public void SingleBitPartsRoundTripExactPatterns()
    {
        float value = PortableMath.ComposeSingle(true, 0x7E, 0x654321U);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(value), Is.EqualTo(0xBF654321U));
            Assert.That(PortableMath.SingleSignBit(value), Is.True);
            Assert.That(PortableMath.SingleExponentBits(value), Is.EqualTo(0x7E));
            Assert.That(PortableMath.SingleFractionBits(value), Is.EqualTo(0x654321U));
            Assert.That(PortableMath.SingleBits(PortableMath.SingleFromBits(0x80000000U)), Is.EqualTo(0x80000000U));
        });
    }

    [Test]
    public void CompositionRejectsRawPartsOutsideTheirWidths()
    {
        AssertArgument("exponentBits", () => PortableMath.ComposeDouble(false, -1, 0UL));
        AssertArgument("exponentBits", () => PortableMath.ComposeDouble(false, 0x800, 0UL));
        AssertArgument("fractionBits", () => PortableMath.ComposeDouble(false, 0, 1UL << 52));
        AssertArgument("exponentBits", () => PortableMath.ComposeSingle(false, 0x100, 0U));
        AssertArgument("fractionBits", () => PortableMath.ComposeSingle(false, 0, 1U << 23));
    }

    [Test]
    public void ScalingCoversNormalSubnormalOverflowAndTieToEven()
    {
        double minimumSubnormal = PortableMath.DoubleFromBits(1UL);
        double threeSubnormals = PortableMath.DoubleFromBits(3UL);
        double negativeMinimum = PortableMath.DoubleFromBits(PortableMath.DoubleSignMask | 1UL);
        double minimumNormal = PortableMath.DoubleFromBits(0x0010000000000000UL);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(1.5, 2)), Is.EqualTo(0x4018000000000000UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(double.MaxValue, 1)), Is.EqualTo(0x7FF0000000000000UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(double.MinValue, 1)), Is.EqualTo(0xFFF0000000000000UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(minimumNormal, -1)), Is.EqualTo(0x0008000000000000UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(minimumSubnormal, 1)), Is.EqualTo(2UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(minimumSubnormal, -1)), Is.EqualTo(0UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(threeSubnormals, -1)), Is.EqualTo(2UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(negativeMinimum, -1)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(1.0, int.MaxValue)), Is.EqualTo(0x7FF0000000000000UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(-1.0, int.MinValue)), Is.EqualTo(PortableMath.DoubleSignMask));
        });
    }

    [Test]
    public void SingleScalingHasStableBoundaryBits()
    {
        float minimumSubnormal = PortableMath.SingleFromBits(1U);
        float threeSubnormals = PortableMath.SingleFromBits(3U);
        float minimumNormal = PortableMath.SingleFromBits(0x00800000U);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(PortableMath.ScaleByPowerOfTwo(1.5f, 2)), Is.EqualTo(0x40C00000U));
            Assert.That(PortableMath.SingleBits(PortableMath.ScaleByPowerOfTwo(float.MaxValue, 1)), Is.EqualTo(0x7F800000U));
            Assert.That(PortableMath.SingleBits(PortableMath.ScaleByPowerOfTwo(minimumNormal, -1)), Is.EqualTo(0x00400000U));
            Assert.That(PortableMath.SingleBits(PortableMath.ScaleByPowerOfTwo(minimumSubnormal, -1)), Is.EqualTo(0U));
            Assert.That(PortableMath.SingleBits(PortableMath.ScaleByPowerOfTwo(threeSubnormals, -1)), Is.EqualTo(2U));
        });
    }

    [Test]
    public void NearestEvenRoundingPreservesTiesSignedZeroAndLargeIntegers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableMath.RoundToNearestEven(0.5)), Is.EqualTo(0UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.RoundToNearestEven(-0.5)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(PortableMath.RoundToNearestEven(1.5), Is.EqualTo(2.0));
            Assert.That(PortableMath.RoundToNearestEven(2.5), Is.EqualTo(2.0));
            Assert.That(PortableMath.RoundToNearestEven(3.5), Is.EqualTo(4.0));
            Assert.That(PortableMath.RoundToNearestEven(-2.5), Is.EqualTo(-2.0));
            Assert.That(PortableMath.RoundToNearestEven(4503599627370496.0), Is.EqualTo(4503599627370496.0));
            Assert.That(PortableMath.SingleBits(PortableMath.RoundToNearestEven(-0.5f)), Is.EqualTo(PortableMath.SingleSignMask));
            Assert.That(PortableMath.RoundToNearestEven(6.5f), Is.EqualTo(6.0f));
            Assert.That(PortableMath.RoundToNearestEven(7.5f), Is.EqualTo(8.0f));
        });
    }

    [Test]
    public void SquareRootMatchesPortableKnownBitFixtures()
    {
        double negativeZero = PortableMath.DoubleFromBits(PortableMath.DoubleSignMask);
        double minimumSubnormal = PortableMath.DoubleFromBits(1UL);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableMath.Sqrt(2.0)), Is.EqualTo(0x3FF6A09E667F3BCDUL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Sqrt(4.0)), Is.EqualTo(0x4000000000000000UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Sqrt(minimumSubnormal)), Is.EqualTo(0x1E60000000000000UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Sqrt(negativeZero)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(PortableMath.DoubleBits(PortableMath.Sqrt(-1.0)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.DoubleBits(PortableMath.Sqrt(double.NaN)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.Sqrt(double.PositiveInfinity), Is.EqualTo(double.PositiveInfinity));
            Assert.That(PortableMath.SingleBits(PortableMath.Sqrt(2.0f)), Is.EqualTo(0x3FB504F3U));
            Assert.That(PortableMath.SingleBits(PortableMath.Sqrt(-1.0f)), Is.EqualTo(PortableMath.SingleCanonicalNaN));
        });
    }

    [Test]
    public void SquareRootMatchesIeeeReferenceAcrossDeterministicBitSweep()
    {
        ulong state = 0xD1B54A32D192ED03UL;
        for (int index = 0; index < 4096; index++)
        {
            state = state * 0x9E3779B97F4A7C15UL + 0xBF58476D1CE4E5B9UL;
            ulong bits = state & 0x7FEFFFFFFFFFFFFFUL;
            double value = PortableMath.DoubleFromBits(bits);
            ulong expected = PortableMath.DoubleBits(Math.Sqrt(value));
            ulong actual = PortableMath.DoubleBits(PortableMath.Sqrt(value));
            Assert.That(actual, Is.EqualTo(expected), $"inputBits={bits:X16}");
        }
    }

    [Test]
    public void BoundarySteppingHandlesZerosFiniteExtremesAndInfinities()
    {
        double negativeZero = PortableMath.DoubleFromBits(PortableMath.DoubleSignMask);
        float negativeZeroSingle = PortableMath.SingleFromBits(PortableMath.SingleSignMask);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableMath.NextUp(0.0)), Is.EqualTo(1UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.NextUp(negativeZero)), Is.EqualTo(1UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.NextDown(0.0)), Is.EqualTo(PortableMath.DoubleSignMask | 1UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.NextUp(-1.0)), Is.EqualTo(0xBFEFFFFFFFFFFFFFUL));
            Assert.That(PortableMath.DoubleBits(PortableMath.NextDown(1.0)), Is.EqualTo(0x3FEFFFFFFFFFFFFFUL));
            Assert.That(PortableMath.NextUp(double.MaxValue), Is.EqualTo(double.PositiveInfinity));
            Assert.That(PortableMath.NextDown(-double.MaxValue), Is.EqualTo(double.NegativeInfinity));
            Assert.That(PortableMath.NextUp(double.PositiveInfinity), Is.EqualTo(double.PositiveInfinity));
            Assert.That(PortableMath.NextDown(double.NegativeInfinity), Is.EqualTo(double.NegativeInfinity));
            Assert.That(PortableMath.SingleBits(PortableMath.NextUp(negativeZeroSingle)), Is.EqualTo(1U));
            Assert.That(PortableMath.SingleBits(PortableMath.NextDown(0.0f)), Is.EqualTo(PortableMath.SingleSignMask | 1U));
        });
    }

    [Test]
    public void NaNResultsUseCanonicalPortablePayloads()
    {
        double unusualNaN = PortableMath.DoubleFromBits(0xFFF0000000000001UL);
        float unusualSingleNaN = PortableMath.SingleFromBits(0xFF800001U);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableMath.ScaleByPowerOfTwo(unusualNaN, 5)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.DoubleBits(PortableMath.RoundToNearestEven(unusualNaN)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.DoubleBits(PortableMath.NextUp(unusualNaN)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.SingleBits(PortableMath.ScaleByPowerOfTwo(unusualSingleNaN, 5)), Is.EqualTo(PortableMath.SingleCanonicalNaN));
            Assert.That(PortableMath.SingleBits(PortableMath.RoundToNearestEven(unusualSingleNaN)), Is.EqualTo(PortableMath.SingleCanonicalNaN));
            Assert.That(PortableMath.SingleBits(PortableMath.NextDown(unusualSingleNaN)), Is.EqualTo(PortableMath.SingleCanonicalNaN));
        });
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
