using System.Collections;

namespace random.Tests;

public class ResamplingTests
{
    [Test]
    public void BootstrapIndexesMatchUniformIndexDrawsExactly()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        int[] expected = new int[257];
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = referenceGenerator.NextInt32Bounded(17);
        }

        int[] actual = Resampling.BootstrapIndexes(actualGenerator, 17, expected.Length);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void StratifiedIndexesMatchIndependentStratumReferenceExactly()
    {
        double[] weights = [0.0, 1.0, 2.0, 7.0, 0.0];
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator referenceGenerator = Create(202UL);

        int[] actual = Resampling.StratifiedIndexes(actualGenerator, weights, 211);
        int[] expected = ReferenceStratified(referenceGenerator, weights, 211);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Has.None.EqualTo(0));
            Assert.That(actual, Has.None.EqualTo(4));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SystematicIndexesMatchSingleOffsetReferenceExactly()
    {
        double[] weights = [1.0, 0.0, 3.0, 6.0];
        RandomGenerator actualGenerator = Create(303UL);
        RandomGenerator referenceGenerator = Create(303UL);

        int[] actual = Resampling.SystematicIndexes(actualGenerator, weights, 199);
        int[] expected = ReferenceSystematic(referenceGenerator, weights, 199);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Has.None.EqualTo(1));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ResidualIndexesMatchFloorPlusFractionalReferenceExactly()
    {
        double[] weights = [0.15, 0.0, 0.35, 0.5];
        RandomGenerator actualGenerator = Create(404UL);
        RandomGenerator referenceGenerator = Create(404UL);

        int[] actual = Resampling.ResidualIndexes(actualGenerator, weights, 37);
        int[] expected = ReferenceResidual(referenceGenerator, weights, 37);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Has.None.EqualTo(1));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void EveryResamplerReturnsAndFillsTheExactRequestedCount()
    {
        double[] weights = [1.0, 2.0, 7.0];
        int[] counts = [0, 1, 37, 100];
        foreach (int count in counts)
        {
            int[] bootstrap = Resampling.BootstrapIndexes(Create(5UL), weights.Length, count);
            int[] stratified = Resampling.StratifiedIndexes(Create(5UL), weights, count);
            int[] systematic = Resampling.SystematicIndexes(Create(5UL), weights, count);
            int[] residual = Resampling.ResidualIndexes(Create(5UL), weights, count);
            Assert.Multiple(() =>
            {
                Assert.That(bootstrap, Has.Length.EqualTo(count));
                Assert.That(stratified, Has.Length.EqualTo(count));
                Assert.That(systematic, Has.Length.EqualTo(count));
                Assert.That(residual, Has.Length.EqualTo(count));
            });
        }

        int[] bootstrapFill = new int[23];
        int[] stratifiedFill = new int[23];
        int[] systematicFill = new int[23];
        int[] residualFill = new int[23];
        Resampling.FillBootstrapIndexes(Create(6UL), weights.Length, bootstrapFill);
        Resampling.FillStratifiedIndexes(Create(6UL), weights, stratifiedFill);
        Resampling.FillSystematicIndexes(Create(6UL), weights, systematicFill);
        Resampling.FillResidualIndexes(Create(6UL), weights, residualFill);
        Assert.Multiple(() =>
        {
            Assert.That(bootstrapFill, Is.All.InRange(0, weights.Length - 1));
            Assert.That(stratifiedFill, Is.All.InRange(0, weights.Length - 1));
            Assert.That(systematicFill, Is.All.InRange(0, weights.Length - 1));
            Assert.That(residualFill, Is.All.InRange(0, weights.Length - 1));
        });
    }

    [Test]
    public void FillMethodsAndCollectionFacadesMatchCoreMethodsExactly()
    {
        double[] weights = [1.0, 2.0, 0.0, 7.0];
        Assert.That(FillBootstrap(505UL, 4, 73), Is.EqualTo(RandomCollections.BootstrapIndexes(Create(505UL), 4, 73)));
        Assert.That(FillStratified(606UL, weights, 73), Is.EqualTo(RandomCollections.StratifiedResample(Create(606UL), weights, 73)));
        Assert.That(FillSystematic(707UL, weights, 73), Is.EqualTo(RandomCollections.SystematicResample(Create(707UL), weights, 73)));
        Assert.That(FillResidual(808UL, weights, 73), Is.EqualTo(RandomCollections.ResidualResample(Create(808UL), weights, 73)));
    }

    [Test]
    public void SinglePositiveCategoryAndEmptyOutputsConsumeNoState()
    {
        RandomGenerator generator = Create(909UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        int[] bootstrap = Resampling.BootstrapIndexes(generator, 1, 25);
        int[] stratified = Resampling.StratifiedIndexes(generator, [0.0, 5.0, 0.0], 25);
        int[] systematic = Resampling.SystematicIndexes(generator, [0.0, 5.0, 0.0], 25);
        int[] residual = Resampling.ResidualIndexes(generator, [0.0, 5.0, 0.0], 25);
        Resampling.FillStratifiedIndexes(generator, [1.0, 2.0], Span<int>.Empty);
        Resampling.FillSystematicIndexes(generator, [1.0, 2.0], Span<int>.Empty);
        Resampling.FillResidualIndexes(generator, [1.0, 2.0], Span<int>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(bootstrap, Is.All.Zero);
            Assert.That(stratified, Is.All.EqualTo(1));
            Assert.That(systematic, Is.All.EqualTo(1));
            Assert.That(residual, Is.All.EqualTo(1));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void StratificationPreservesExactIntegerAllocationsAndFiniteScaling()
    {
        double[] weights = [1.0, 2.0, 7.0];
        int[] stratified = Resampling.StratifiedIndexes(Create(1001UL), weights, 100);
        int[] systematic = Resampling.SystematicIndexes(Create(1002UL), weights, 100);

        Assert.Multiple(() =>
        {
            Assert.That(stratified.Count(value => value == 0), Is.EqualTo(10));
            Assert.That(stratified.Count(value => value == 1), Is.EqualTo(20));
            Assert.That(stratified.Count(value => value == 2), Is.EqualTo(70));
            Assert.That(systematic.Count(value => value == 0), Is.EqualTo(10));
            Assert.That(systematic.Count(value => value == 1), Is.EqualTo(20));
            Assert.That(systematic.Count(value => value == 2), Is.EqualTo(70));
        });

        Assert.That(Resampling.StratifiedIndexes(Create(1003UL), weights, 101),
            Is.EqualTo(Resampling.StratifiedIndexes(Create(1003UL), [16.0, 32.0, 112.0], 101)));
        Assert.That(Resampling.SystematicIndexes(Create(1004UL), weights, 101),
            Is.EqualTo(Resampling.SystematicIndexes(Create(1004UL), [16.0, 32.0, 112.0], 101)));
        Assert.That(Resampling.ResidualIndexes(Create(1005UL), weights, 101),
            Is.EqualTo(Resampling.ResidualIndexes(Create(1005UL), [16.0, 32.0, 112.0], 101)));
    }

    [Test]
    public void BootstrapSamplesPreserveSourcesTypesAndArrayBoundsSemantics()
    {
        Array source = Array.CreateInstance(typeof(string), [4], [-2]);
        source.SetValue("a", -2);
        source.SetValue("b", -1);
        source.SetValue("c", 0);
        source.SetValue("d", 1);
        Array snapshot = (Array)source.Clone();
        RandomGenerator arrayGenerator = Create(1101UL);
        RandomGenerator referenceGenerator = Create(1101UL);
        int[] indexes = Resampling.BootstrapIndexes(referenceGenerator, source.Length, 41);

        Array sample = RandomCollections.BootstrapSample(arrayGenerator, source, 41);
        string[] expected = indexes.Select(index => ReflectionInfo.Required<string>(source.GetValue(index - 2))).ToArray();
        IList list = new ArrayList() { "a", "b", "c", "d" };
        object?[] listSample = RandomCollections.BootstrapSample(Create(1201UL), list, 41);

        Assert.Multiple(() =>
        {
            Assert.That(sample, Is.TypeOf<string[]>());
            Assert.That(sample.GetLowerBound(0), Is.Zero);
            Assert.That(sample.Cast<string>(), Is.EqualTo(expected));
            Assert.That(source.Cast<string>(), Is.EqualTo(snapshot.Cast<string>()));
            Assert.That(arrayGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            Assert.That(listSample, Has.Length.EqualTo(41));
            Assert.That(listSample.All(value => list.Contains(value)), Is.True);
        });
    }

    [Test]
    public void ExplicitBootstrapOverloadsRetainArrayTypes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new bool[] { true }, 2), Is.TypeOf<bool[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new byte[] { 1 }, 2), Is.TypeOf<byte[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new sbyte[] { 1 }, 2), Is.TypeOf<sbyte[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new short[] { 1 }, 2), Is.TypeOf<short[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new ushort[] { 1 }, 2), Is.TypeOf<ushort[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new int[] { 1 }, 2), Is.TypeOf<int[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new uint[] { 1 }, 2), Is.TypeOf<uint[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new long[] { 1 }, 2), Is.TypeOf<long[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new ulong[] { 1 }, 2), Is.TypeOf<ulong[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new char[] { 'x' }, 2), Is.TypeOf<char[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new float[] { 1 }, 2), Is.TypeOf<float[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new double[] { 1 }, 2), Is.TypeOf<double[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new decimal[] { 1 }, 2), Is.TypeOf<decimal[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new string[] { "x" }, 2), Is.TypeOf<string[]>());
            Assert.That(RandomCollections.BootstrapSample(Create(1UL), new object[] { "x" }, 2), Is.TypeOf<object[]>());
        });
    }

    [Test]
    public void InvalidInputsFailBeforeConsumptionOrDestinationMutation()
    {
        RandomGenerator generator = Create(1301UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        int[] destination = [9, 9, 9];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => Resampling.BootstrapIndexes(NullReference.Value<global::random.IRandomSource>(), 2, 1)), "generator" },
            new object[] { (TestDelegate)(() => Resampling.BootstrapIndexes(generator, 0, 1)), "populationSize" },
            new object[] { (TestDelegate)(() => Resampling.BootstrapIndexes(generator, 2, -1)), "count" },
            new object[] { (TestDelegate)(() => Resampling.StratifiedIndexes(generator, NullReference.Value<double[]>(), 1)), "weights" },
            new object[] { (TestDelegate)(() => Resampling.SystematicIndexes(generator, [], 1)), "weights" },
            new object[] { (TestDelegate)(() => Resampling.ResidualIndexes(generator, [0.0, 0.0], 1)), "weights" },
            new object[] { (TestDelegate)(() => Resampling.StratifiedIndexes(generator, [1.0, -1.0], 1)), "weights" },
            new object[] { (TestDelegate)(() => Resampling.SystematicIndexes(generator, [double.NaN], 1)), "weights" },
            new object[] { (TestDelegate)(() => Resampling.ResidualIndexes(generator, [double.PositiveInfinity], 1)), "weights" },
            new object[] { (TestDelegate)(() => Resampling.StratifiedIndexes(generator, [1.0], -1)), "count" },
            new object[] { (TestDelegate)(() => Resampling.FillResidualIndexes(generator, [1.0, -1.0], destination)), "weights" },
            new object[] { (TestDelegate)(() => RandomCollections.BootstrapSample(generator, Array.Empty<int>(), 1)), "values" },
            new object[] { (TestDelegate)(() => RandomCollections.BootstrapSample(generator, new int[] { 1 }, -1)), "count" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new int[] { 9, 9, 9 }));
        }
    }

    static int[] FillBootstrap(ulong seed, int populationSize, int count)
    {
        int[] result = new int[count];
        RandomCollections.FillBootstrapIndexes(Create(seed), populationSize, result);
        return result;
    }

    static int[] FillStratified(ulong seed, double[] weights, int count)
    {
        int[] result = new int[count];
        RandomCollections.FillStratifiedResample(Create(seed), weights, result);
        return result;
    }

    static int[] FillSystematic(ulong seed, double[] weights, int count)
    {
        int[] result = new int[count];
        RandomCollections.FillSystematicResample(Create(seed), weights, result);
        return result;
    }

    static int[] FillResidual(ulong seed, double[] weights, int count)
    {
        int[] result = new int[count];
        RandomCollections.FillResidualResample(Create(seed), weights, result);
        return result;
    }

    static int[] ReferenceStratified(RandomGenerator generator, double[] weights, int count)
    {
        CategoricalProbabilityTable table = new CategoricalProbabilityTable(weights, "weights");
        int[] result = new int[count];
        for (int index = 0; index < result.Length; index++)
        {
            double position = ((double)index + generator.NextDoubleClosedOpen()) / result.Length;
            result[index] = Find(table, position);
        }

        return result;
    }

    static int[] ReferenceSystematic(RandomGenerator generator, double[] weights, int count)
    {
        CategoricalProbabilityTable table = new CategoricalProbabilityTable(weights, "weights");
        int[] result = new int[count];
        double offset = generator.NextDoubleClosedOpen();
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = Find(table, (offset + index) / result.Length);
        }

        return result;
    }

    static int[] ReferenceResidual(RandomGenerator generator, double[] weights, int count)
    {
        CategoricalProbabilityTable table = new CategoricalProbabilityTable(weights, "weights");
        int[] result = new int[count];
        double[] residualWeights = new double[weights.Length];
        int output = 0;
        for (int category = 0; category < weights.Length; category++)
        {
            double expected = table.NormalizedProbability(category) * count;
            int deterministicCount = (int)Math.Floor(expected);
            residualWeights[category] = expected - deterministicCount;
            for (int copy = 0; copy < deterministicCount; copy++)
            {
                result[output++] = category;
            }
        }

        if (output < result.Length)
        {
            CategoricalProbabilityTable residualTable = new CategoricalProbabilityTable(residualWeights, "weights");
            while (output < result.Length)
            {
                result[output++] = Find(residualTable, generator.NextDoubleClosedOpen());
            }
        }

        return result;
    }

    static int Find(CategoricalProbabilityTable table, double position)
    {
        int category = 0;
        while (category < table.Length() - 1 && position >= table.CumulativeProbability(category))
        {
            category++;
        }

        return category;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
