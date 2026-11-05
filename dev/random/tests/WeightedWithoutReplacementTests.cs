namespace random.Tests;

public class WeightedWithoutReplacementTests
{
    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void FiniteWeightsMatchLogExponentialKeyReferenceExactly(RandomMathMode mathMode)
    {
        double[] weights = [double.Epsilon, 1.0, double.MaxValue, 0.0, 1.0e-200, 1.0e200];
        RandomGenerator actualGenerator = Create(101UL, mathMode);
        RandomGenerator referenceGenerator = Create(101UL, mathMode);

        for (int repetition = 0; repetition < 257; repetition++)
        {
            int[] actual = WeightedWithoutReplacement.SampleIndexes(actualGenerator, weights, 4);
            int[] expected = ReferenceFiniteSample(referenceGenerator, weights, 4, mathMode);
            Assert.That(actual, Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FirstPositionFrequenciesRespectWeightsAndExcludeZeros()
    {
        RandomGenerator generator = Create(202UL);
        double[] weights = [0.0, 1.0, 2.0, 0.0, 7.0];
        int[] firstCounts = new int[weights.Length];
        const int sampleCount = 100000;
        for (int repetition = 0; repetition < sampleCount; repetition++)
        {
            int[] sample = WeightedWithoutReplacement.SampleIndexes(generator, weights, 2);
            firstCounts[sample[0]]++;
            Assert.That(sample[1], Is.Not.EqualTo(sample[0]));
        }

        Assert.Multiple(() =>
        {
            Assert.That(firstCounts[0], Is.Zero);
            Assert.That(firstCounts[1], Is.InRange(9200, 10800));
            Assert.That(firstCounts[2], Is.InRange(19200, 20800));
            Assert.That(firstCounts[3], Is.Zero);
            Assert.That(firstCounts[4], Is.InRange(69200, 70800));
        });
    }

    [Test]
    public void SmallFiniteSamplesReachEveryOrderedPair()
    {
        RandomGenerator generator = Create(303UL);
        HashSet<string> pairs = new HashSet<string>();
        for (int repetition = 0; repetition < 3000; repetition++)
        {
            pairs.Add(string.Join(",", WeightedWithoutReplacement.SampleIndexes(generator, [1.0, 1.0, 1.0, 1.0], 2)));
        }

        Assert.That(pairs.Count, Is.EqualTo(12));
    }

    [Test]
    public void InfiniteWeightsAreUniformlyOrderedBeforeFiniteWeights()
    {
        double[] weights = [double.PositiveInfinity, 1.0, double.PositiveInfinity, 0.0, double.PositiveInfinity, 5.0];
        RandomGenerator actualGenerator = Create(404UL);
        RandomGenerator referenceGenerator = Create(404UL);
        int[] infiniteIndexes = [0, 2, 4];
        RandomCollections.PartialShuffle(referenceGenerator, infiniteIndexes, 2);

        int[] actual = WeightedWithoutReplacement.SampleIndexes(actualGenerator, weights, 2);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(infiniteIndexes.Take(2)));
            Assert.That(actual, Is.SubsetOf(new int[] { 0, 2, 4 }));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InfiniteWeightOrderingReachesEveryPossiblePair()
    {
        RandomGenerator generator = Create(505UL);
        HashSet<string> pairs = new HashSet<string>();
        double[] weights = [double.PositiveInfinity, 1.0, double.PositiveInfinity, double.PositiveInfinity];
        for (int repetition = 0; repetition < 1000; repetition++)
        {
            int[] sample = WeightedWithoutReplacement.SampleIndexes(generator, weights, 2);
            pairs.Add(string.Join(",", sample));
            Assert.That(sample, Is.SubsetOf(new int[] { 0, 2, 3 }));
        }

        Assert.That(pairs.Count, Is.EqualTo(6));
    }

    [Test]
    public void InfiniteWeightsOccupyThePrefixWhenFiniteIndexesAreAlsoNeeded()
    {
        RandomGenerator generator = Create(606UL);
        double[] weights = [double.PositiveInfinity, 1.0, 4.0, double.PositiveInfinity, 0.0];

        for (int repetition = 0; repetition < 1000; repetition++)
        {
            int[] sample = WeightedWithoutReplacement.SampleIndexes(generator, weights, 4);
            Assert.Multiple(() =>
            {
                Assert.That(sample.Take(2), Is.EquivalentTo(new int[] { 0, 3 }));
                Assert.That(sample.Skip(2), Is.EquivalentTo(new int[] { 1, 2 }));
            });
        }
    }

    [Test]
    public void DeterministicEligibleIndexesConsumeNoState()
    {
        RandomGenerator generator = Create(707UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        int[] finite = WeightedWithoutReplacement.SampleIndexes(generator, [0.0, 5.0, 0.0], 1);
        int[] mixed = WeightedWithoutReplacement.SampleIndexes(generator, [double.PositiveInfinity, 0.0, 3.0], 2);
        int[] zero = WeightedWithoutReplacement.SampleIndexes(generator, [0.0, 0.0], 0);
        int[] empty = WeightedWithoutReplacement.SampleIndexes(generator, [], 0);

        Assert.Multiple(() =>
        {
            Assert.That(finite, Is.EqualTo(new int[] { 1 }));
            Assert.That(mixed, Is.EqualTo(new int[] { 0, 2 }));
            Assert.That(zero, Is.Empty);
            Assert.That(empty, Is.Empty);
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void FiniteScaleFactorsDoNotChangeSampleOrdering(RandomMathMode mathMode)
    {
        RandomGenerator baseGenerator = Create(808UL, mathMode);
        RandomGenerator scaledGenerator = Create(808UL, mathMode);
        for (int repetition = 0; repetition < 1000; repetition++)
        {
            int[] baseSample = WeightedWithoutReplacement.SampleIndexes(baseGenerator, [1.0, 2.0, 4.0, 8.0], 3);
            int[] scaledSample = WeightedWithoutReplacement.SampleIndexes(scaledGenerator, [16.0, 32.0, 64.0, 128.0], 3);
            Assert.That(baseSample, Is.EqualTo(scaledSample));
        }

        Assert.That(baseGenerator.ExportState().ToBytes(), Is.EqualTo(scaledGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FillAndFacadesMatchRepeatedSamplesExactly()
    {
        double[] weights = [1.0, 2.0, 0.0, 7.0, double.PositiveInfinity, double.PositiveInfinity];
        RandomGenerator fillGenerator = Create(909UL);
        RandomGenerator scalarGenerator = Create(909UL);
        RandomGenerator facadeGenerator = Create(909UL);
        RandomGenerator facadeFillGenerator = Create(909UL);
        int[] filled = new int[4];
        int[] facadeFilled = new int[4];

        WeightedWithoutReplacement.FillIndexes(fillGenerator, weights, filled);
        int[] scalar = WeightedWithoutReplacement.SampleIndexes(scalarGenerator, weights, filled.Length);
        int[] alias = WeightedWithoutReplacement.Sample(facadeGenerator, weights, filled.Length);
        RandomCollections.FillWeightedSampleWithoutReplacement(facadeFillGenerator, weights, facadeFilled);
        int[] facade = RandomCollections.WeightedSampleWithoutReplacement(Create(909UL), weights, filled.Length);

        Assert.Multiple(() =>
        {
            Assert.That(filled, Is.EqualTo(scalar));
            Assert.That(alias, Is.EqualTo(scalar));
            Assert.That(facadeFilled, Is.EqualTo(scalar));
            Assert.That(facade, Is.EqualTo(scalar));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
            Assert.That(facadeGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
            Assert.That(facadeFillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void EmptyFillsValidateWeightsButConsumeNoState()
    {
        RandomGenerator generator = Create(1001UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        WeightedWithoutReplacement.FillIndexes(generator, [0.0, double.PositiveInfinity], Span<int>.Empty);
        RandomCollections.FillWeightedSampleWithoutReplacement(generator, [], Span<int>.Empty);

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void InvalidInputsFailBeforeConsumptionAndFillMutation()
    {
        RandomGenerator generator = Create(1102UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        int[] destination = [9, 9];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => WeightedWithoutReplacement.SampleIndexes(NullReference.Value<global::random.IRandomSource>(), [1.0], 1)), "generator" },
            new object[] { (TestDelegate)(() => WeightedWithoutReplacement.SampleIndexes(generator, NullReference.Value<double[]>(), 1)), "weights" },
            new object[] { (TestDelegate)(() => WeightedWithoutReplacement.SampleIndexes(generator, [1.0], -1)), "count" },
            new object[] { (TestDelegate)(() => WeightedWithoutReplacement.SampleIndexes(generator, [1.0, double.NaN], 1)), "weights" },
            new object[] { (TestDelegate)(() => WeightedWithoutReplacement.SampleIndexes(generator, [1.0, -double.Epsilon], 1)), "weights" },
            new object[] { (TestDelegate)(() => WeightedWithoutReplacement.SampleIndexes(generator, [double.NegativeInfinity], 1)), "weights" },
            new object[] { (TestDelegate)(() => WeightedWithoutReplacement.SampleIndexes(generator, [0.0, 1.0], 2)), "count" },
            new object[] { (TestDelegate)(() => WeightedWithoutReplacement.FillIndexes(generator, [1.0], destination)), "count" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }

        Assert.That(destination, Is.EqualTo(new int[] { 9, 9 }));
    }

    static int[] ReferenceFiniteSample(RandomGenerator generator, double[] weights, int count, RandomMathMode mathMode)
    {
        int[] indexes = Enumerable.Range(0, weights.Length).Where(index => weights[index] > 0.0).ToArray();
        double[] keys = new double[indexes.Length];
        for (int index = 0; index < indexes.Length; index++)
        {
            double unit = generator.NextDoubleOpenOpen();
            double firstLog = mathMode == RandomMathMode.Portable ? PortableMath.Log(unit) : Math.Log(unit);
            double exponential = -firstLog;
            double exponentialLog = mathMode == RandomMathMode.Portable ? PortableMath.Log(exponential) : Math.Log(exponential);
            double weightLog = mathMode == RandomMathMode.Portable ? PortableMath.Log(weights[indexes[index]]) : Math.Log(weights[indexes[index]]);
            keys[index] = exponentialLog - weightLog;
        }

        Array.Sort(keys, indexes);
        return indexes.Take(count).ToArray();
    }

    static RandomGenerator Create(ulong seed)
    {
        return Create(seed, RandomMathMode.Portable);
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mathMode);
    }
}
