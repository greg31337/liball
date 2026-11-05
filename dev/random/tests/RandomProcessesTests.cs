namespace random.Tests;

public class RandomProcessesTests
{
    [Test]
    public void IntegerWalkMatchesDirectionBitsExactly()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        long[] expected = new long[65];
        expected[0] = 17L;
        for (int step = 1; step < expected.Length; step++)
        {
            expected[step] = expected[step - 1] + (referenceGenerator.NextBoolean() ? 3L : -3L);
        }

        long[] actual = RandomProcesses.RandomWalk(actualGenerator, 17L, 3L, 64);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void FloatingWalkMatchesDirectionBitsExactly()
    {
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator referenceGenerator = Create(202UL);
        double[] expected = new double[38];
        expected[0] = -1.25;
        for (int step = 1; step < expected.Length; step++)
        {
            expected[step] = expected[step - 1] + (referenceGenerator.NextBoolean() ? 0.125 : -0.125);
        }

        double[] actual = RandomProcesses.RandomWalk(actualGenerator, -1.25, 0.125, 37);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void IntegerBatchIsPathMajorAndMatchesRepeatedWalks()
    {
        const int pathCount = 7;
        const int stepCount = 19;
        RandomGenerator batchGenerator = Create(303UL);
        RandomGenerator repeatedGenerator = Create(303UL);
        long[] expected = new long[pathCount * (stepCount + 1)];
        for (int path = 0; path < pathCount; path++)
        {
            long[] onePath = RandomProcesses.RandomWalk(repeatedGenerator, -5L, 2L, stepCount);
            onePath.CopyTo(expected, path * (stepCount + 1));
        }

        long[] actual = RandomProcesses.RandomWalks(batchGenerator, -5L, 2L, pathCount, stepCount);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
            Assert.That(actual.Where((value, index) => index % (stepCount + 1) == 0), Is.All.EqualTo(-5L));
        });
    }

    [Test]
    public void FloatingBatchIsPathMajorAndMatchesRepeatedWalks()
    {
        const int pathCount = 5;
        const int stepCount = 23;
        RandomGenerator batchGenerator = Create(404UL);
        RandomGenerator repeatedGenerator = Create(404UL);
        double[] expected = new double[pathCount * (stepCount + 1)];
        for (int path = 0; path < pathCount; path++)
        {
            double[] onePath = RandomProcesses.RandomWalk(repeatedGenerator, 2.5, 0.25, stepCount);
            onePath.CopyTo(expected, path * (stepCount + 1));
        }

        double[] actual = RandomProcesses.RandomWalks(batchGenerator, 2.5, 0.25, pathCount, stepCount);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
            Assert.That(actual.Where((value, index) => index % (stepCount + 1) == 0), Is.All.EqualTo(2.5));
        });
    }

    [Test]
    public void AllocatingAndDestinationOverloadsReplayExactly()
    {
        RandomGenerator integerAllocating = Create(505UL);
        RandomGenerator integerDestination = Create(505UL);
        long[] integerExpected = new long[RandomProcesses.RandomWalkBatchOutputLength(4, 12)];
        long[] integerActual = RandomProcesses.RandomWalks(integerAllocating, 10L, 4L, 4, 12);
        RandomProcesses.RandomWalks(integerDestination, 10L, 4L, 4, 12, integerExpected);

        RandomGenerator floatingAllocating = Create(506UL);
        RandomGenerator floatingDestination = Create(506UL);
        double[] floatingExpected = new double[RandomProcesses.RandomWalkOutputLength(29)];
        double[] floatingActual = RandomProcesses.RandomWalk(floatingAllocating, -3.0, 0.5, 29);
        RandomProcesses.RandomWalk(floatingDestination, -3.0, 0.5, 29, floatingExpected);

        Assert.Multiple(() =>
        {
            Assert.That(integerActual, Is.EqualTo(integerExpected));
            Assert.That(integerAllocating.ExportState().ToBytes(), Is.EqualTo(integerDestination.ExportState().ToBytes()));
            Assert.That(floatingActual, Is.EqualTo(floatingExpected));
            Assert.That(floatingAllocating.ExportState().ToBytes(), Is.EqualTo(floatingDestination.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ZeroStepsAndEmptyBatchesConsumeNoRandomState()
    {
        RandomGenerator generator = Create(606UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        long[] integers = RandomProcesses.RandomWalks(generator, -11L, 0L, 3, 8);
        double[] floating = RandomProcesses.RandomWalk(generator, -0.0, 0.0, 12);
        long[] noSteps = RandomProcesses.RandomWalk(generator, 7L, 4L, 0);
        double[] empty = RandomProcesses.RandomWalks(generator, 1.0, 0.5, 0, 20);

        Assert.Multiple(() =>
        {
            Assert.That(integers, Is.All.EqualTo(-11L));
            Assert.That(floating.Select(BitConverter.DoubleToUInt64Bits), Is.All.EqualTo(0x8000000000000000UL));
            Assert.That(noSteps, Is.EqualTo(new long[] { 7L }));
            Assert.That(empty, Is.Empty);
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void OutputLengthMetadataIncludesInitialStatesAndRejectsOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RandomProcesses.RandomWalkOutputLength(0), Is.EqualTo(1));
            Assert.That(RandomProcesses.RandomWalkOutputLength(17), Is.EqualTo(18));
            Assert.That(RandomProcesses.RandomWalkBatchOutputLength(0, 17), Is.Zero);
            Assert.That(RandomProcesses.RandomWalkBatchOutputLength(5, 17), Is.EqualTo(90));
        });

        AssertArgument("stepCount", () => RandomProcesses.RandomWalkOutputLength(-1));
        AssertArgument("stepCount", () => RandomProcesses.RandomWalkOutputLength(int.MaxValue));
        AssertArgument("pathCount", () => RandomProcesses.RandomWalkBatchOutputLength(-1, 2));
        AssertArgument("pathCount", () => RandomProcesses.RandomWalkBatchOutputLength(int.MaxValue, 1));
    }

    [Test]
    public void InvalidWalkRequestsAreAtomic()
    {
        AssertArgument("generator", () => RandomProcesses.RandomWalk(NullReference.Value<global::random.RandomGenerator>(), 0L, 1L, 1));
        AssertArgument("generator", () => RandomProcesses.RandomWalk(NullReference.Value<global::random.RandomGenerator>(), 0.0, 1.0, 1));
        AssertArgument("initialState", () => RandomProcesses.RandomWalk(Create(701UL), double.NaN, 1.0, 1));
        AssertArgument("stepSize", () => RandomProcesses.RandomWalk(Create(702UL), 0L, -1L, 1));
        AssertArgument("stepSize", () => RandomProcesses.RandomWalk(Create(703UL), 0.0, -1.0, 1));
        AssertArgument("stepSize", () => RandomProcesses.RandomWalk(Create(704UL), 0.0, double.PositiveInfinity, 1));

        RandomGenerator generator = Create(705UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        long[] integerDestination = [2L, 3L, 5L];
        double[] floatingDestination = [7.0, 11.0, 13.0];
        AssertArgument("stepSize", () => RandomProcesses.RandomWalk(generator, long.MaxValue, 1L, 1, integerDestination.AsSpan(0, 2)));
        AssertArgument("stepSize", () => RandomProcesses.RandomWalk(generator, double.MaxValue, double.MaxValue, 1, floatingDestination.AsSpan(0, 2)));
        AssertArgument("destination", () => RandomProcesses.RandomWalk(generator, 0L, 1L, 3, integerDestination));
        AssertArgument("destination", () => RandomProcesses.RandomWalk(generator, 0.0, 1.0, 3, floatingDestination));

        Assert.Multiple(() =>
        {
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(integerDestination, Is.EqualTo(new long[] { 2L, 3L, 5L }));
            Assert.That(floatingDestination, Is.EqualTo(new double[] { 7.0, 11.0, 13.0 }));
        });
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
