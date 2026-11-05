using System.Numerics;

namespace random.Tests;

public class BigIntegerUniformTests
{
    [Test]
    public void SplitMixBytesProduceLockedBoundedVector()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        BigInteger[] expected = [5, 3, 2, 4, 2, 6, 4, 5];

        foreach (BigInteger value in expected)
        {
            Assert.That(generator.NextBigInteger(10), Is.EqualTo(value));
        }

        Assert.That(generator.NextUInt64(), Is.EqualTo(11408980392250668974UL));
    }

    [Test]
    public void RejectionPathConsumesOnlyRejectedCandidates()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        BigInteger[] expected = [3, 82, 6];

        foreach (BigInteger value in expected)
        {
            Assert.That(generator.NextBigInteger(129), Is.EqualTo(value));
        }

        Assert.That(generator.NextUInt64(), Is.EqualTo(4028864712777624925UL));
    }

    [Test]
    public void NonByteAlignedPowerOfTwoUsesExactBitLengthMask()
    {
        RandomGenerator actual = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        RandomGenerator reference = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        BigInteger bound = BigInteger.One << 2049;
        byte[] expectedBytes = reference.NextBytes(257);
        expectedBytes[256] &= 1;
        BigInteger expected = new BigInteger(expectedBytes, true, false);

        BigInteger result = actual.NextBigInteger(bound);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(result, Is.GreaterThanOrEqualTo(BigInteger.Zero));
            Assert.That(result, Is.LessThan(bound));
            Assert.That(actual.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void VeryLargeNegativeAndCrossZeroRangesRemainValid()
    {
        RandomGenerator generator = RandomGenerator.CreatePcg64Dxsm((UInt128)123, (UInt128)456, RandomMathMode.Portable);
        BigInteger negativeMinimum = -(BigInteger.One << 16384);
        BigInteger negativeMaximum = -(BigInteger.One << 16000);
        BigInteger crossMinimum = -(BigInteger.One << 12000) - 123;
        BigInteger crossMaximum = (BigInteger.One << 12345) + 456;

        for (int index = 0; index < 32; index++)
        {
            BigInteger negative = generator.NextBigIntegerInRange(negativeMinimum, negativeMaximum);
            BigInteger crossZero = generator.NextBigIntegerInRange(crossMinimum, crossMaximum);
            Assert.Multiple(() =>
            {
                Assert.That(negative, Is.GreaterThanOrEqualTo(negativeMinimum));
                Assert.That(negative, Is.LessThan(negativeMaximum));
                Assert.That(crossZero, Is.GreaterThanOrEqualTo(crossMinimum));
                Assert.That(crossZero, Is.LessThan(crossMaximum));
            });
        }
    }

    [Test]
    public void SingleValueDomainsConsumeNothingIncludingBooleanReservoir()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        BigInteger value = -(BigInteger.One << 4096);

        Assert.Multiple(() =>
        {
            Assert.That(generator.NextBigInteger(BigInteger.One), Is.EqualTo(BigInteger.Zero));
            Assert.That(generator.NextBigIntegerInRange(value, value + BigInteger.One), Is.EqualTo(value));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidBoundsAreRejectedBeforeStateChanges()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(123UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        BigInteger huge = BigInteger.One << 4096;
        object[] cases =
        [
            new object[] { (TestDelegate)(() => generator.NextBigInteger(BigInteger.Zero)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextBigInteger(-huge)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextBigIntegerInRange(huge, huge)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextBigIntegerInRange(huge, -huge)), "maximum" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void OutputsAlwaysRemainInsideRequestedArbitraryRanges()
    {
        RandomGenerator generator = RandomGenerator.CreateXoshiro256StarStar(987654321UL, RandomMathMode.Portable);
        BigInteger bound = (BigInteger.One << 521) + (BigInteger.One << 127) + 1;
        BigInteger minimum = -(BigInteger.One << 777) + 19;
        BigInteger maximum = (BigInteger.One << 900) - 33;

        for (int index = 0; index < 1000; index++)
        {
            BigInteger bounded = generator.NextBigInteger(bound);
            BigInteger ranged = generator.NextBigIntegerInRange(minimum, maximum);
            Assert.Multiple(() =>
            {
                Assert.That(bounded, Is.GreaterThanOrEqualTo(BigInteger.Zero));
                Assert.That(bounded, Is.LessThan(bound));
                Assert.That(ranged, Is.GreaterThanOrEqualTo(minimum));
                Assert.That(ranged, Is.LessThan(maximum));
            });
        }
    }
}
