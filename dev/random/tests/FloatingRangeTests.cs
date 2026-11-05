namespace random.Tests;

public class FloatingRangeTests
{
    [Test]
    public void InterpolationHandlesExactAndOverflowingWidths()
    {
        float largestSingleUnit = FloatingUniform.SingleFromNumerator(FloatingUniform.SingleGridSize - 1U);
        double largestDoubleUnit = FloatingUniform.DoubleFromNumerator(FloatingUniform.DoubleGridSize - 1UL);

        Assert.Multiple(() =>
        {
            Assert.That(FloatingUniform.InterpolateSingleClosedOpen(-4.0f, 12.0f, 0.25f), Is.EqualTo(0.0f));
            Assert.That(FloatingUniform.InterpolateDoubleClosedOpen(-4.0, 12.0, 0.25), Is.EqualTo(0.0));
            Assert.That(FloatingUniform.InterpolateSingleClosedOpen(float.MinValue, float.MaxValue, 0.5f), Is.EqualTo(0.0f));
            Assert.That(FloatingUniform.InterpolateDoubleClosedOpen(double.MinValue, double.MaxValue, 0.5), Is.EqualTo(0.0));
            Assert.That(FloatingUniform.InterpolateSingleClosedOpen(float.MinValue, float.MaxValue, largestSingleUnit), Is.LessThan(float.MaxValue));
            Assert.That(FloatingUniform.InterpolateDoubleClosedOpen(double.MinValue, double.MaxValue, largestDoubleUnit), Is.LessThan(double.MaxValue));
        });
    }

    [Test]
    public void PublicRangesMatchUnitInterpolationAndStateConsumption()
    {
        RandomGenerator actualSingle = Create();
        RandomGenerator referenceSingle = Create();
        RandomGenerator actualDouble = Create();
        RandomGenerator referenceDouble = Create();
        float expectedSingle = FloatingUniform.InterpolateSingleClosedOpen(-123.5f, 987.25f, referenceSingle.NextSingle());
        double expectedDouble = FloatingUniform.InterpolateDoubleClosedOpen(-1.0e200, 7.0e199, referenceDouble.NextDouble());

        float actualSingleValue = actualSingle.NextSingleInRange(-123.5f, 987.25f);
        double actualDoubleValue = actualDouble.NextDoubleInRange(-1.0e200, 7.0e199);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(actualSingleValue), Is.EqualTo(PortableMath.SingleBits(expectedSingle)));
            Assert.That(actualSingle.ExportState().ToBytes(), Is.EqualTo(referenceSingle.ExportState().ToBytes()));
            Assert.That(PortableMath.DoubleBits(actualDoubleValue), Is.EqualTo(PortableMath.DoubleBits(expectedDouble)));
            Assert.That(actualDouble.ExportState().ToBytes(), Is.EqualTo(referenceDouble.ExportState().ToBytes()));
        });
    }

    [Test]
    public void DegenerateRangesReturnMinimumBitsWithoutConsumption()
    {
        RandomGenerator generator = Create();
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        float negativeSingleZero = PortableMath.SingleFromBits(PortableMath.SingleSignMask);
        double negativeDoubleZero = PortableMath.DoubleFromBits(PortableMath.DoubleSignMask);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(generator.NextSingleInRange(17.25f, 17.25f)), Is.EqualTo(PortableMath.SingleBits(17.25f)));
            Assert.That(PortableMath.SingleBits(generator.NextSingleInRange(negativeSingleZero, 0.0f)), Is.EqualTo(PortableMath.SingleSignMask));
            Assert.That(PortableMath.DoubleBits(generator.NextDoubleInRange(-91.5, -91.5)), Is.EqualTo(PortableMath.DoubleBits(-91.5)));
            Assert.That(PortableMath.DoubleBits(generator.NextDoubleInRange(negativeDoubleZero, 0.0)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void AdjacentRangesCannotRoundToExcludedMaximum()
    {
        RandomGenerator generator = Create();
        float singleMinimum = 1.0f;
        float singleMaximum = PortableMath.NextUp(singleMinimum);
        double doubleMinimum = 1.0;
        double doubleMaximum = PortableMath.NextUp(doubleMinimum);
        float minimumSingleSubnormal = PortableMath.SingleFromBits(1U);
        double minimumDoubleSubnormal = PortableMath.DoubleFromBits(1UL);

        for (int index = 0; index < 1000; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.SingleBits(generator.NextSingleInRange(singleMinimum, singleMaximum)), Is.EqualTo(PortableMath.SingleBits(singleMinimum)));
                Assert.That(PortableMath.DoubleBits(generator.NextDoubleInRange(doubleMinimum, doubleMaximum)), Is.EqualTo(PortableMath.DoubleBits(doubleMinimum)));
                Assert.That(PortableMath.SingleBits(generator.NextSingleInRange(0.0f, minimumSingleSubnormal)), Is.EqualTo(0U));
                Assert.That(PortableMath.DoubleBits(generator.NextDoubleInRange(0.0, minimumDoubleSubnormal)), Is.EqualTo(0UL));
            });
        }
    }

    [Test]
    public void InvalidRangesAreRejectedBeforeStateChanges()
    {
        RandomGenerator generator = Create();
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => generator.NextSingleInRange(float.NaN, 1.0f)), "minimum" },
            new object[] { (TestDelegate)(() => generator.NextSingleInRange(0.0f, float.PositiveInfinity)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextSingleInRange(2.0f, 1.0f)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextDoubleInRange(double.NegativeInfinity, 1.0)), "minimum" },
            new object[] { (TestDelegate)(() => generator.NextDoubleInRange(0.0, double.NaN)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextDoubleInRange(2.0, 1.0)), "maximum" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void FullFiniteRangesAlwaysRemainFiniteAndInsideBounds()
    {
        RandomGenerator generator = RandomGenerator.CreatePcg64Dxsm((UInt128)123, (UInt128)456, RandomMathMode.Portable);
        for (int index = 0; index < 10000; index++)
        {
            float single = generator.NextSingleInRange(float.MinValue, float.MaxValue);
            double doubleValue = generator.NextDoubleInRange(double.MinValue, double.MaxValue);
            Assert.Multiple(() =>
            {
                Assert.That(float.IsFinite(single), Is.True);
                Assert.That(single, Is.GreaterThanOrEqualTo(float.MinValue));
                Assert.That(single, Is.LessThan(float.MaxValue));
                Assert.That(double.IsFinite(doubleValue), Is.True);
                Assert.That(doubleValue, Is.GreaterThanOrEqualTo(double.MinValue));
                Assert.That(doubleValue, Is.LessThan(double.MaxValue));
            });
        }
    }

    static RandomGenerator Create()
    {
        return RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
    }
}
