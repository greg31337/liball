namespace random.Tests;

public class WeightedSamplerTests
{
    [TestCase(WeightedSamplingMethod.Cumulative)]
    [TestCase(WeightedSamplingMethod.GuideTable)]
    [TestCase(WeightedSamplingMethod.AliasTable)]
    public void PublicConfiguredFactoryMatchesCanonicalTraversalAndCopiesWeights(WeightedSamplingMethod method)
    {
        IRandomFactory factory = new RandomFactory();
        RandomGenerator actualGenerator = Create(11UL);
        RandomGenerator expectedGenerator = Create(11UL);
        double[] weights = [1.0, 0.0, 3.0, 6.0];
        WeightedSampler actual = factory.CreateWeightedSamplerWithMethod(actualGenerator, weights, method);
        WeightedSampler expected = WeightedSampler.FromWeights(expectedGenerator, [1.0, 0.0, 3.0, 6.0], method);
        weights[0] = double.NaN;
        weights[2] = 0.0;
        int[] actualValues = new int[257];
        int[] expectedValues = new int[257];

        actual.FillIndices(actualValues);
        expected.FillIndices(expectedValues);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Method(), Is.EqualTo(method));
            Assert.That(actual.CategoryCount(), Is.EqualTo(4));
            Assert.That(actualValues, Is.EqualTo(expectedValues));
            Assert.That(actualValues, Has.None.EqualTo(1));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void PublicConvenienceFactoriesMatchTheirConfiguredMethods()
    {
        IRandomFactory factory = new RandomFactory();
        double[] weights = [1.0, 2.0, 0.0, 7.0];
        RandomGenerator[] actualGenerators = [Create(21UL), Create(22UL), Create(23UL)];
        RandomGenerator[] expectedGenerators = [Create(21UL), Create(22UL), Create(23UL)];
        WeightedSampler[] actual =
        [
            factory.CreateWeightedSampler(actualGenerators[0], weights),
            factory.CreateGuideWeightedSampler(actualGenerators[1], weights),
            factory.CreateAliasWeightedSampler(actualGenerators[2], weights)
        ];
        WeightedSampler[] expected =
        [
            WeightedSampler.FromWeights(expectedGenerators[0], [1.0, 2.0, 0.0, 7.0]),
            WeightedSampler.FromGuideWeights(expectedGenerators[1], [1.0, 2.0, 0.0, 7.0]),
            WeightedSampler.FromAliasWeights(expectedGenerators[2], [1.0, 2.0, 0.0, 7.0])
        ];
        weights[0] = double.PositiveInfinity;

        for (int samplerIndex = 0; samplerIndex < actual.Length; samplerIndex++)
        {
            int[] actualValues = new int[129];
            int[] expectedValues = new int[129];
            actual[samplerIndex].FillIndices(actualValues);
            expected[samplerIndex].FillIndices(expectedValues);
            Assert.Multiple(() =>
            {
                Assert.That(actualValues, Is.EqualTo(expectedValues));
                Assert.That(actualValues, Has.None.EqualTo(2));
                Assert.That(actualGenerators[samplerIndex].ExportState().ToBytes(), Is.EqualTo(expectedGenerators[samplerIndex].ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void PublicTableFactoriesReuseTraversalAndSupportAtomicUpdates()
    {
        IRandomFactory factory = new RandomFactory();
        GuideTable guide = new GuideTable([1.0, 4.0, 0.0, 5.0]);
        AliasTable alias = new AliasTable([1.0, 4.0, 0.0, 5.0]);
        RandomGenerator guideGenerator = Create(31UL);
        RandomGenerator guideReference = Create(31UL);
        RandomGenerator aliasGenerator = Create(32UL);
        RandomGenerator aliasReference = Create(32UL);
        WeightedSampler guideSampler = factory.CreateWeightedSamplerFromGuideTable(guideGenerator, guide);
        WeightedSampler aliasSampler = factory.CreateWeightedSamplerFromAliasTable(aliasGenerator, alias);

        for (int index = 0; index < 257; index++)
        {
            Assert.That(guideSampler.SampleIndex(), Is.EqualTo(guide.Sample(guideReference)));
            Assert.That(aliasSampler.SampleIndex(), Is.EqualTo(alias.Sample(aliasReference)));
        }

        byte[] guideBefore = guideGenerator.ExportState().ToBytes();
        byte[] aliasBefore = aliasGenerator.ExportState().ToBytes();
        Assert.Throws<RandomArgumentException>(() => guideSampler.UpdateWeights([double.NaN]));
        Assert.Throws<RandomArgumentException>(() => aliasSampler.UpdateWeights([double.NaN]));
        guideSampler.UpdateWeights([0.0, 1.0]);
        aliasSampler.UpdateWeights([1.0, 0.0]);

        Assert.Multiple(() =>
        {
            Assert.That(guideSampler.Method(), Is.EqualTo(WeightedSamplingMethod.GuideTable));
            Assert.That(aliasSampler.Method(), Is.EqualTo(WeightedSamplingMethod.AliasTable));
            Assert.That(guideSampler.SampleIndex(), Is.EqualTo(1));
            Assert.That(aliasSampler.SampleIndex(), Is.EqualTo(0));
            Assert.That(guideGenerator.ExportState().ToBytes(), Is.EqualTo(guideBefore));
            Assert.That(aliasGenerator.ExportState().ToBytes(), Is.EqualTo(aliasBefore));
        });
    }

    [Test]
    public void PublicFactoryFailuresReportArgumentsBeforeConsumption()
    {
        IRandomFactory factory = new RandomFactory();
        RandomGenerator generator = Create(41UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        AssertArgument("weights", () => factory.CreateWeightedSampler(generator, []));
        AssertArgument("method", () => factory.CreateWeightedSamplerWithMethod(generator, [1.0], (WeightedSamplingMethod)99));
        AssertArgument("weights", () => factory.CreateGuideWeightedSampler(generator, [0.0]));
        AssertArgument("weights", () => factory.CreateAliasWeightedSampler(generator, [double.NaN]));
        AssertArgument("table", () => factory.CreateWeightedSamplerFromGuideTable(generator, NullReference.Value<global::random.GuideTable>()));
        AssertArgument("table", () => factory.CreateWeightedSamplerFromAliasTable(generator, NullReference.Value<global::random.AliasTable>()));

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void CumulativeMethodMatchesCategoricalSamplerExactly()
    {
        double[] weights = [0.0, 1.0, 2.0, 0.0, 7.0];
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        WeightedSampler actual = WeightedSampler.FromWeights(actualGenerator, weights);
        CategoricalSampler reference = new CategoricalSampler(referenceGenerator, weights);

        for (int index = 0; index < 2000; index++)
        {
            Assert.That(actual.SampleIndex(), Is.EqualTo(reference.Sample()));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual.Method(), Is.EqualTo(WeightedSamplingMethod.Cumulative));
            Assert.That(actual.CategoryCount(), Is.EqualTo(weights.Length));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void GuideMethodMatchesGuideTableExactly()
    {
        double[] weights = [5.0, 0.0, 2.0, 3.0];
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator referenceGenerator = Create(202UL);
        WeightedSampler actual = WeightedSampler.FromGuideWeights(actualGenerator, weights);
        GuideTable reference = new GuideTable(weights);

        for (int index = 0; index < 2000; index++)
        {
            Assert.That(actual.SampleIndex(), Is.EqualTo(reference.Sample(referenceGenerator)));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual.Method(), Is.EqualTo(WeightedSamplingMethod.GuideTable));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void AliasMethodMatchesAliasTableExactly()
    {
        double[] weights = [5.0, 0.0, 2.0, 3.0];
        RandomGenerator actualGenerator = Create(303UL);
        RandomGenerator referenceGenerator = Create(303UL);
        WeightedSampler actual = WeightedSampler.FromAliasWeights(actualGenerator, weights);
        AliasTable reference = new AliasTable(weights);

        for (int index = 0; index < 2000; index++)
        {
            Assert.That(actual.SampleIndex(), Is.EqualTo(reference.Sample(referenceGenerator)));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual.Method(), Is.EqualTo(WeightedSamplingMethod.AliasTable));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void PreprocessedTableFactoriesReuseExactTableTraversal()
    {
        GuideTable guide = new GuideTable([1.0, 4.0, 0.0, 5.0]);
        AliasTable alias = new AliasTable([1.0, 4.0, 0.0, 5.0]);
        RandomGenerator guideGenerator = Create(404UL);
        RandomGenerator guideReferenceGenerator = Create(404UL);
        RandomGenerator aliasGenerator = Create(505UL);
        RandomGenerator aliasReferenceGenerator = Create(505UL);
        WeightedSampler guideSampler = WeightedSampler.FromGuideTable(guideGenerator, guide);
        WeightedSampler aliasSampler = WeightedSampler.FromAliasTable(aliasGenerator, alias);

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(guideSampler.SampleIndex(), Is.EqualTo(guide.Sample(guideReferenceGenerator)));
            Assert.That(aliasSampler.SampleIndex(), Is.EqualTo(alias.Sample(aliasReferenceGenerator)));
        }

        Assert.Multiple(() =>
        {
            Assert.That(guideSampler.CategoryCount(), Is.EqualTo(4));
            Assert.That(aliasSampler.CategoryCount(), Is.EqualTo(4));
            Assert.That(guideGenerator.ExportState().ToBytes(), Is.EqualTo(guideReferenceGenerator.ExportState().ToBytes()));
            Assert.That(aliasGenerator.ExportState().ToBytes(), Is.EqualTo(aliasReferenceGenerator.ExportState().ToBytes()));
        });
    }

    [TestCase(WeightedSamplingMethod.Cumulative)]
    [TestCase(WeightedSamplingMethod.GuideTable)]
    [TestCase(WeightedSamplingMethod.AliasTable)]
    public void FillMatchesRepeatedScalarSamplesExactly(WeightedSamplingMethod method)
    {
        RandomGenerator fillGenerator = Create(606UL);
        RandomGenerator scalarGenerator = Create(606UL);
        WeightedSampler fillSampler = WeightedSampler.FromWeights(fillGenerator, [1.0, 2.0, 0.0, 7.0], method);
        WeightedSampler scalarSampler = WeightedSampler.FromWeights(scalarGenerator, [1.0, 2.0, 0.0, 7.0], method);
        int[] actual = new int[257];
        int[] expected = new int[actual.Length];

        fillSampler.FillIndices(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = scalarSampler.SampleIndex();
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Has.None.EqualTo(2));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    [TestCase(WeightedSamplingMethod.Cumulative)]
    [TestCase(WeightedSamplingMethod.GuideTable)]
    [TestCase(WeightedSamplingMethod.AliasTable)]
    public void WeightedFrequenciesRespectMassAndExcludeZeroWeights(WeightedSamplingMethod method)
    {
        WeightedSampler sampler = WeightedSampler.FromWeights(Create(707UL), [0.0, 1.0, 2.0, 0.0, 7.0], method);
        int[] counts = new int[5];
        const int sampleCount = 100000;
        for (int index = 0; index < sampleCount; index++)
        {
            counts[sampler.SampleIndex()]++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(counts[0], Is.Zero);
            Assert.That(counts[1], Is.InRange(9200, 10800));
            Assert.That(counts[2], Is.InRange(19200, 20800));
            Assert.That(counts[3], Is.Zero);
            Assert.That(counts[4], Is.InRange(69200, 70800));
        });
    }

    [TestCase(WeightedSamplingMethod.Cumulative)]
    [TestCase(WeightedSamplingMethod.GuideTable)]
    [TestCase(WeightedSamplingMethod.AliasTable)]
    public void SinglePositiveWeightAndEmptyFillsConsumeNoState(WeightedSamplingMethod method)
    {
        RandomGenerator generator = Create(808UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        WeightedSampler sampler = WeightedSampler.FromWeights(generator, [0.0, 0.0, double.MaxValue, 0.0], method);
        int[] values = new int[32];

        int scalar = sampler.SampleIndex();
        sampler.FillIndices(values);
        sampler.FillIndices(Span<int>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(scalar, Is.EqualTo(2));
            Assert.That(values, Is.All.EqualTo(2));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [TestCase(WeightedSamplingMethod.Cumulative)]
    [TestCase(WeightedSamplingMethod.GuideTable)]
    [TestCase(WeightedSamplingMethod.AliasTable)]
    public void UpdatesReplaceWeightsWithoutChangingMethodOrConsumingState(WeightedSamplingMethod method)
    {
        RandomGenerator actualGenerator = Create(909UL);
        RandomGenerator referenceGenerator = Create(909UL);
        WeightedSampler actual = WeightedSampler.FromWeights(actualGenerator, [1.0, 1.0], method);
        byte[] before = actualGenerator.ExportState().ToBytes();
        actual.UpdateWeights([0.0, 2.0, 3.0, 0.0]);
        WeightedSampler reference = WeightedSampler.FromWeights(referenceGenerator, [0.0, 2.0, 3.0, 0.0], method);

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        for (int index = 0; index < 1000; index++)
        {
            Assert.That(actual.SampleIndex(), Is.EqualTo(reference.SampleIndex()));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual.Method(), Is.EqualTo(method));
            Assert.That(actual.CategoryCount(), Is.EqualTo(4));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [TestCase(WeightedSamplingMethod.Cumulative)]
    [TestCase(WeightedSamplingMethod.GuideTable)]
    [TestCase(WeightedSamplingMethod.AliasTable)]
    public void InvalidUpdatesAreAtomic(WeightedSamplingMethod method)
    {
        RandomGenerator actualGenerator = Create(1001UL);
        RandomGenerator referenceGenerator = Create(1001UL);
        WeightedSampler actual = WeightedSampler.FromWeights(actualGenerator, [1.0, 2.0, 3.0], method);
        WeightedSampler reference = WeightedSampler.FromWeights(referenceGenerator, [1.0, 2.0, 3.0], method);
        byte[] before = actualGenerator.ExportState().ToBytes();

        RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => actual.UpdateWeights([1.0, double.NaN]));

        Assert.That(error.ArgumentName(), Is.EqualTo("weights"));
        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        for (int index = 0; index < 1000; index++)
        {
            Assert.That(actual.SampleIndex(), Is.EqualTo(reference.SampleIndex()));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [TestCase(WeightedSamplingMethod.Cumulative)]
    [TestCase(WeightedSamplingMethod.GuideTable)]
    [TestCase(WeightedSamplingMethod.AliasTable)]
    public void WeightSnapshotsAreDefensiveAndScaleInvariant(WeightedSamplingMethod method)
    {
        double[] weights = [1.0, 2.0, 0.0, 7.0];
        RandomGenerator actualGenerator = Create(1102UL);
        RandomGenerator referenceGenerator = Create(1102UL);
        WeightedSampler actual = WeightedSampler.FromWeights(actualGenerator, weights, method);
        WeightedSampler reference = WeightedSampler.FromWeights(referenceGenerator, [16.0, 32.0, 0.0, 112.0], method);

        weights[0] = double.NaN;
        weights[1] = 0.0;
        weights[3] = double.PositiveInfinity;
        for (int index = 0; index < 1000; index++)
        {
            Assert.That(actual.SampleIndex(), Is.EqualTo(reference.SampleIndex()));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void ConvenienceFactoriesMatchExplicitMethodSelection()
    {
        RandomGenerator guideGenerator = Create(1203UL);
        RandomGenerator explicitGuideGenerator = Create(1203UL);
        RandomGenerator aliasGenerator = Create(1304UL);
        RandomGenerator explicitAliasGenerator = Create(1304UL);
        WeightedSampler guide = WeightedSampler.FromGuideWeights(guideGenerator, [1.0, 2.0, 3.0]);
        WeightedSampler explicitGuide = WeightedSampler.FromWeights(explicitGuideGenerator, [1.0, 2.0, 3.0], WeightedSamplingMethod.GuideTable);
        WeightedSampler alias = WeightedSampler.FromAliasWeights(aliasGenerator, [1.0, 2.0, 3.0]);
        WeightedSampler explicitAlias = WeightedSampler.FromWeights(explicitAliasGenerator, [1.0, 2.0, 3.0], WeightedSamplingMethod.AliasTable);

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(guide.SampleIndex(), Is.EqualTo(explicitGuide.SampleIndex()));
            Assert.That(alias.SampleIndex(), Is.EqualTo(explicitAlias.SampleIndex()));
        }

        Assert.Multiple(() =>
        {
            Assert.That(guideGenerator.ExportState().ToBytes(), Is.EqualTo(explicitGuideGenerator.ExportState().ToBytes()));
            Assert.That(aliasGenerator.ExportState().ToBytes(), Is.EqualTo(explicitAliasGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InvalidFactoriesReportArgumentsWithoutConsumingState()
    {
        RandomGenerator generator = Create(1405UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => WeightedSampler.FromWeights(NullReference.Value<global::random.IRandomSource>(), [1.0])), "generator" },
            new object[] { (TestDelegate)(() => WeightedSampler.FromWeights(generator, NullReference.Value<double[]>())), "weights" },
            new object[] { (TestDelegate)(() => WeightedSampler.FromWeights(generator, [], WeightedSamplingMethod.Cumulative)), "weights" },
            new object[] { (TestDelegate)(() => WeightedSampler.FromWeights(generator, [0.0, 0.0], WeightedSamplingMethod.GuideTable)), "weights" },
            new object[] { (TestDelegate)(() => WeightedSampler.FromWeights(generator, [1.0, -double.Epsilon], WeightedSamplingMethod.AliasTable)), "weights" },
            new object[] { (TestDelegate)(() => WeightedSampler.FromWeights(generator, [double.PositiveInfinity])), "weights" },
            new object[] { (TestDelegate)(() => WeightedSampler.FromWeights(generator, [1.0], (WeightedSamplingMethod)99)), "method" },
            new object[] { (TestDelegate)(() => WeightedSampler.FromGuideTable(generator, NullReference.Value<global::random.GuideTable>())), "table" },
            new object[] { (TestDelegate)(() => WeightedSampler.FromAliasTable(generator, NullReference.Value<global::random.AliasTable>())), "table" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertArgument(string argumentName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(argumentName));
    }
}
