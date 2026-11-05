namespace random.Tests;

public class IntegerUniform32Tests
{
    [Test]
    public void PublishedMtWordsProduceLockedBoundedVector()
    {
        RandomGenerator generator = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        uint[] expected = [8U, 1U, 9U, 8U, 1U, 9U, 9U, 2U];

        foreach (uint value in expected)
        {
            Assert.That(generator.NextUInt32Bounded(10U), Is.EqualTo(value));
        }
    }

    [Test]
    public void RejectionPathConsumesOnlyRejectedWords()
    {
        RandomGenerator generator = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        uint bound = 2147483649U;
        uint[] expected = [1749605806U, 1945173367U, 474666992U, 1357981149U, 661783701U];

        foreach (uint value in expected)
        {
            Assert.That(generator.NextUInt32Bounded(bound), Is.EqualTo(value));
        }

        Assert.That(generator.NextUInt32(), Is.EqualTo(418932835U));
    }

    [Test]
    public void ThresholdMatchesFullDomainRemainderAtBoundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IntegerUniform.RejectionThreshold(1U), Is.EqualTo(0U));
            Assert.That(IntegerUniform.RejectionThreshold(2U), Is.EqualTo(0U));
            Assert.That(IntegerUniform.RejectionThreshold(3U), Is.EqualTo(1U));
            Assert.That(IntegerUniform.RejectionThreshold(2147483648U), Is.EqualTo(0U));
            Assert.That(IntegerUniform.RejectionThreshold(2147483649U), Is.EqualTo(2147483647U));
            Assert.That(IntegerUniform.RejectionThreshold(uint.MaxValue), Is.EqualTo(1U));
        });
    }

    [Test]
    public void ReducedDomainMultiplyHighRejectionIsExhaustivelyUniform()
    {
        for (int sourceBits = 1; sourceBits <= 8; sourceBits++)
        {
            int domainSize = 1 << sourceBits;
            int mask = domainSize - 1;
            for (int bound = 1; bound <= domainSize; bound++)
            {
                int threshold = domainSize % bound;
                int[] counts = new int[bound];
                int accepted = 0;
                for (int sample = 0; sample < domainSize; sample++)
                {
                    int product = sample * bound;
                    int low = product & mask;
                    if (low >= threshold)
                    {
                        counts[product >> sourceBits]++;
                        accepted++;
                    }
                }

                Assert.That(accepted % bound, Is.EqualTo(0));
                int expectedCount = accepted / bound;
                for (int result = 0; result < counts.Length; result++)
                {
                    Assert.That(counts[result], Is.EqualTo(expectedCount), $"sourceBits={sourceBits}, bound={bound}, result={result}");
                }
            }
        }
    }

    [Test]
    public void UnsignedRangesHandleZeroAndMaximumEdges()
    {
        RandomGenerator maximumWidth = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        RandomGenerator highRange = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);

        Assert.Multiple(() =>
        {
            Assert.That(maximumWidth.NextUInt32InRange(0U, uint.MaxValue), Is.EqualTo(3499211611U));
            uint high = highRange.NextUInt32InRange(uint.MaxValue - 100U, uint.MaxValue);
            Assert.That(high, Is.GreaterThanOrEqualTo(uint.MaxValue - 100U));
            Assert.That(high, Is.LessThan(uint.MaxValue));
        });
    }

    [Test]
    public void SignedRangesHandleNegativeAndNearlyFullDomains()
    {
        RandomGenerator negative = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        RandomGenerator nearlyFull = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);

        Assert.Multiple(() =>
        {
            Assert.That(negative.NextInt32InRange(-10, 10), Is.EqualTo(6));
            Assert.That(nearlyFull.NextInt32InRange(int.MinValue, int.MaxValue), Is.EqualTo(1351727963));
            Assert.That(RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable).NextInt32Bounded(10), Is.EqualTo(8));
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
            Assert.That(generator.NextUInt32Bounded(1U), Is.EqualTo(0U));
            Assert.That(generator.NextUInt32InRange(123U, 124U), Is.EqualTo(123U));
            Assert.That(generator.NextInt32Bounded(1), Is.EqualTo(0));
            Assert.That(generator.NextInt32InRange(-7, -6), Is.EqualTo(-7));
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
            new object[] { (TestDelegate)(() => generator.NextUInt32Bounded(0U)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextUInt32InRange(5U, 5U)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextUInt32InRange(6U, 5U)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextInt32Bounded(0)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextInt32Bounded(-1)), "bound" },
            new object[] { (TestDelegate)(() => generator.NextInt32InRange(5, 5)), "maximum" },
            new object[] { (TestDelegate)(() => generator.NextInt32InRange(6, 5)), "maximum" }
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
            uint unsigned = generator.NextUInt32InRange(17U, 4000000000U);
            int signed = generator.NextInt32InRange(-2000000000, 2000000000);
            Assert.Multiple(() =>
            {
                Assert.That(unsigned, Is.GreaterThanOrEqualTo(17U));
                Assert.That(unsigned, Is.LessThan(4000000000U));
                Assert.That(signed, Is.GreaterThanOrEqualTo(-2000000000));
                Assert.That(signed, Is.LessThan(2000000000));
            });
        }
    }
}
