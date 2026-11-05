namespace random.Tests;

public class IntegerUniform64Tests
{
    [Test]
    public void SplitMixWordsProduceLockedBoundedVector()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        ulong[] expected = [7UL, 1UL, 2UL, 3UL, 0UL, 8UL, 2UL, 8UL];

        foreach (ulong value in expected)
        {
            Assert.That(generator.NextUInt64Bounded(10UL), Is.EqualTo(value));
        }
    }

    [Test]
    public void RejectionPathConsumesOnlyRejectedWords()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        ulong bound = 9223372036854775809UL;
        ulong[] expected =
        [
            1474913046063446145UL,
            8007990562831494531UL,
            2014432356388812462UL,
            7384525663493887954UL,
            3135310438806241002UL
        ];

        foreach (ulong value in expected)
        {
            Assert.That(generator.NextUInt64Bounded(bound), Is.EqualTo(value));
        }

        Assert.That(generator.NextUInt64(), Is.EqualTo(11408980392250668974UL));
    }

    [Test]
    public void ThresholdMatchesFullDomainRemainderAtBoundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IntegerUniform.RejectionThreshold(1UL), Is.EqualTo(0UL));
            Assert.That(IntegerUniform.RejectionThreshold(2UL), Is.EqualTo(0UL));
            Assert.That(IntegerUniform.RejectionThreshold(3UL), Is.EqualTo(1UL));
            Assert.That(IntegerUniform.RejectionThreshold(9223372036854775808UL), Is.EqualTo(0UL));
            Assert.That(IntegerUniform.RejectionThreshold(9223372036854775809UL), Is.EqualTo(9223372036854775807UL));
            Assert.That(IntegerUniform.RejectionThreshold(ulong.MaxValue), Is.EqualTo(1UL));
        });
    }

    [Test]
    public void UInt128ProductExposesExpectedHighAndLowHalves()
    {
        UInt128 product = (UInt128)13679457532755275413UL * (UInt128)9223372036854775809UL;

        Assert.Multiple(() =>
        {
            Assert.That(WideMath.High64(product), Is.EqualTo(6839728766377637707UL));
            Assert.That(WideMath.Low64(product), Is.EqualTo(4456085495900499605UL));
        });
    }

    [Test]
    public void UnsignedRangesHandleZeroAndMaximumEdges()
    {
        RandomGenerator maximumWidth = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        RandomGenerator highRange = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);

        Assert.Multiple(() =>
        {
            Assert.That(maximumWidth.NextUInt64InRange(0UL, ulong.MaxValue), Is.EqualTo(13679457532755275412UL));
            ulong high = highRange.NextUInt64InRange(ulong.MaxValue - 100UL, ulong.MaxValue);
            Assert.That(high, Is.GreaterThanOrEqualTo(ulong.MaxValue - 100UL));
            Assert.That(high, Is.LessThan(ulong.MaxValue));
        });
    }

    [Test]
    public void SignedRangesHandleNegativeAndNearlyFullDomains()
    {
        RandomGenerator negative = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        RandomGenerator nearlyFull = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);

        Assert.Multiple(() =>
        {
            Assert.That(negative.NextInt64InRange(-10L, 10L), Is.EqualTo(4L));
            Assert.That(nearlyFull.NextInt64InRange(long.MinValue, long.MaxValue), Is.EqualTo(4456085495900499604L));
            Assert.That(RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable).NextInt64Bounded(10L), Is.EqualTo(7L));
        });
    }

    [Test]
    public void SingleValueDomainsConsumeNothingIncludingBooleanReservoir()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        Assert.Multiple(() =>
        {
            Assert.That(generator.NextUInt64Bounded(1UL), Is.EqualTo(0UL));
            Assert.That(generator.NextUInt64InRange(ulong.MaxValue - 1UL, ulong.MaxValue), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(generator.NextInt64Bounded(1L), Is.EqualTo(0L));
            Assert.That(generator.NextInt64InRange(long.MinValue, long.MinValue + 1L), Is.EqualTo(long.MinValue));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidBoundsAreRejectedBeforeStateChanges()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(123UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => generator.NextUInt64Bounded(0UL)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextUInt64InRange(5UL, 5UL)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextUInt64InRange(6UL, 5UL)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextInt64Bounded(0L)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextInt64Bounded(-1L)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextInt64InRange(5L, 5L)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextInt64InRange(6L, 5L)), "maximum" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void OutputsAlwaysRemainInsideRequestedRanges()
    {
        RandomGenerator generator = RandomGenerator.CreatePcg64Dxsm((UInt128)123, (UInt128)456, RandomMathMode.Portable);
        for (int index = 0; index < 10000; index++)
        {
            ulong unsigned = generator.NextUInt64InRange(17UL, 18000000000000000000UL);
            long signed = generator.NextInt64InRange(-9000000000000000000L, 9000000000000000000L);
            Assert.Multiple(() =>
            {
                Assert.That(unsigned, Is.GreaterThanOrEqualTo(17UL));
                Assert.That(unsigned, Is.LessThan(18000000000000000000UL));
                Assert.That(signed, Is.GreaterThanOrEqualTo(-9000000000000000000L));
                Assert.That(signed, Is.LessThan(9000000000000000000L));
            });
        }
    }
}
