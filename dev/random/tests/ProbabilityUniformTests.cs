namespace random.Tests;

public class ProbabilityUniformTests
{
    const ulong ZeroOutputSeed = 0x61C8864680B583EBUL;

    [Test]
    public void IeeeProbabilitiesReduceToExactDyadicFractions()
    {
        DyadicProbability doubleHalf = DyadicProbability.FromDouble(0.5);
        DyadicProbability doubleTenth = DyadicProbability.FromDouble(0.1);
        DyadicProbability doubleMinimum = DyadicProbability.FromDouble(double.Epsilon);
        DyadicProbability doubleBelowOne = DyadicProbability.FromDouble(PortableMath.NextDown(1.0));
        DyadicProbability singleTenth = DyadicProbability.FromSingle(0.1f);
        DyadicProbability singleMinimum = DyadicProbability.FromSingle(float.Epsilon);

        Assert.Multiple(() =>
        {
            Assert.That(doubleHalf.Numerator(), Is.EqualTo(1UL));
            Assert.That(doubleHalf.DenominatorBits(), Is.EqualTo(1));
            Assert.That(doubleTenth.Numerator(), Is.EqualTo(3602879701896397UL));
            Assert.That(doubleTenth.DenominatorBits(), Is.EqualTo(55));
            Assert.That(doubleMinimum.Numerator(), Is.EqualTo(1UL));
            Assert.That(doubleMinimum.DenominatorBits(), Is.EqualTo(1074));
            Assert.That(doubleBelowOne.Numerator(), Is.EqualTo(9007199254740991UL));
            Assert.That(doubleBelowOne.DenominatorBits(), Is.EqualTo(53));
            Assert.That(singleTenth.Numerator(), Is.EqualTo(13421773UL));
            Assert.That(singleTenth.DenominatorBits(), Is.EqualTo(27));
            Assert.That(singleMinimum.Numerator(), Is.EqualTo(1UL));
            Assert.That(singleMinimum.DenominatorBits(), Is.EqualTo(149));
        });
    }

    [Test]
    public void ZeroAndOneProbabilitiesConsumeNothingIncludingReservoir()
    {
        RandomGenerator generator = Create();
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        Assert.Multiple(() =>
        {
            Assert.That(generator.NextBooleanWithDoubleProbability(0.0), Is.False);
            Assert.That(generator.NextBooleanWithDoubleProbability(1.0), Is.True);
            Assert.That(generator.NextBooleanWithSingleProbability(0.0f), Is.False);
            Assert.That(generator.NextBooleanWithSingleProbability(1.0f), Is.True);
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidProbabilitiesAreRejectedBeforeStateChanges()
    {
        RandomGenerator generator = Create();
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        TestDelegate[] cases =
        [
            () => generator.NextBooleanWithDoubleProbability(-double.Epsilon),
            () => generator.NextBooleanWithDoubleProbability(PortableMath.NextUp(1.0)),
            () => generator.NextBooleanWithDoubleProbability(double.NaN),
            () => generator.NextBooleanWithSingleProbability(float.NegativeInfinity),
            () => generator.NextBooleanWithSingleProbability(PortableMath.NextUp(1.0f))
        ];

        foreach (TestDelegate testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(testCase);
            Assert.That(error.ArgumentName(), Is.EqualTo("probability"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void LockedThreeQuarterVectorUsesExpectedReservoirBits()
    {
        RandomGenerator generator = Create();
        RandomGenerator reference = Create();
        bool[] expected = [true, true, true, true, true, false, true, false, true, false, true, true];

        foreach (bool value in expected)
        {
            Assert.That(generator.NextBooleanWithDoubleProbability(0.75), Is.EqualTo(value));
        }

        for (int bit = 0; bit < 21; bit++)
        {
            reference.NextBoolean();
        }

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
    }

    [Test]
    public void MinimumSubnormalProbabilityIsNotRoundedToZero()
    {
        RandomGenerator doubleGenerator = RandomGenerator.CreateSplitMix64(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator doubleReference = RandomGenerator.CreateSplitMix64(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator singleGenerator = RandomGenerator.CreateSplitMix64(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator singleReference = RandomGenerator.CreateSplitMix64(ZeroOutputSeed, RandomMathMode.Portable);

        Assert.That(doubleGenerator.NextBooleanWithDoubleProbability(double.Epsilon), Is.False);
        Assert.That(singleGenerator.NextBooleanWithSingleProbability(float.Epsilon), Is.False);
        for (int bit = 0; bit < 65; bit++)
        {
            doubleReference.NextBoolean();
            singleReference.NextBoolean();
        }

        Assert.Multiple(() =>
        {
            Assert.That(doubleGenerator.ExportState().ToBytes(), Is.EqualTo(doubleReference.ExportState().ToBytes()));
            Assert.That(singleGenerator.ExportState().ToBytes(), Is.EqualTo(singleReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SignsAndRademacherValuesMatchSingleReservoirBits()
    {
        RandomGenerator generator = Create();
        RandomGenerator reference = Create();
        for (int index = 0; index < 1000; index++)
        {
            int expectedSign = reference.NextBoolean() ? 1 : -1;
            Assert.That(generator.NextSign(), Is.EqualTo(expectedSign));
            int expectedRademacher = reference.NextBoolean() ? 1 : -1;
            Assert.That(generator.NextRademacher(), Is.EqualTo(expectedRademacher));
        }

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
    }

    [Test]
    public void WeightedBooleansProduceBothOutcomesAtInteriorProbabilities()
    {
        RandomGenerator generator = RandomGenerator.CreatePcg64Dxsm((UInt128)123, (UInt128)456, RandomMathMode.Portable);
        int trueCount = 0;
        int falseCount = 0;
        for (int index = 0; index < 10000; index++)
        {
            if (generator.NextBooleanWithDoubleProbability(0.37))
            {
                trueCount++;
            }
            else
            {
                falseCount++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(trueCount, Is.GreaterThan(0));
            Assert.That(falseCount, Is.GreaterThan(0));
            Assert.That(trueCount + falseCount, Is.EqualTo(10000));
        });
    }

    static RandomGenerator Create()
    {
        return RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
    }
}
