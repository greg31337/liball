namespace random.Tests;

public class CategoricalTableTests
{
    [Test]
    public void GuideTableMatchesCumulativeCategoricalSamplingExactly()
    {
        double[] probabilities = [0.0, 1.0, 2.0, 0.0, 7.0, 0.0];
        RandomGenerator guideGenerator = Create(42UL);
        RandomGenerator categoricalGenerator = Create(42UL);
        GuideTable guide = new GuideTable(probabilities);
        CategoricalSampler categorical = new CategoricalSampler(categoricalGenerator, probabilities);

        for (int index = 0; index < 2000; index++)
        {
            Assert.That(guide.Sample(guideGenerator), Is.EqualTo(categorical.Sample()));
        }

        Assert.That(guideGenerator.ExportState().ToBytes(), Is.EqualTo(categoricalGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void AliasTableRespectsNormalizedMassAndExcludesZeroEntries()
    {
        AliasTable table = new AliasTable([0.0, 1.0, 2.0, 0.0, 7.0, 0.0]);
        RandomGenerator generator = Create(123UL);
        int[] counts = new int[6];
        const int sampleCount = 100000;

        for (int index = 0; index < sampleCount; index++)
        {
            counts[table.Sample(generator)]++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(counts[0], Is.Zero);
            Assert.That(counts[1], Is.InRange(9200, 10800));
            Assert.That(counts[2], Is.InRange(19200, 20800));
            Assert.That(counts[3], Is.Zero);
            Assert.That(counts[4], Is.InRange(69200, 70800));
            Assert.That(counts[5], Is.Zero);
        });
    }

    [Test]
    public void AliasPreprocessingIsScaleInvariantAndOverflowSafe()
    {
        AliasTable baseTable = new AliasTable([1.0, 2.0, 0.0, 7.0]);
        AliasTable scaledTable = new AliasTable([16.0, 32.0, 0.0, 112.0]);
        AliasTable maximumTable = new AliasTable([double.MaxValue, 0.0, double.MaxValue]);
        AliasTable unitTable = new AliasTable([1.0, 0.0, 1.0]);
        RandomGenerator baseGenerator = Create(234UL);
        RandomGenerator scaledGenerator = Create(234UL);
        RandomGenerator maximumGenerator = Create(345UL);
        RandomGenerator unitGenerator = Create(345UL);

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(baseTable.Sample(baseGenerator), Is.EqualTo(scaledTable.Sample(scaledGenerator)));
            Assert.That(maximumTable.Sample(maximumGenerator), Is.EqualTo(unitTable.Sample(unitGenerator)));
        }

        Assert.Multiple(() =>
        {
            Assert.That(baseGenerator.ExportState().ToBytes(), Is.EqualTo(scaledGenerator.ExportState().ToBytes()));
            Assert.That(maximumGenerator.ExportState().ToBytes(), Is.EqualTo(unitGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void UniformAliasTableUsesOnlyTheUniformColumnDraw()
    {
        AliasTable table = new AliasTable([1.0, 1.0, 1.0, 1.0]);
        RandomGenerator actualGenerator = Create(456UL);
        RandomGenerator referenceGenerator = Create(456UL);

        for (int index = 0; index < 257; index++)
        {
            Assert.That(table.Sample(actualGenerator), Is.EqualTo(referenceGenerator.NextInt32Bounded(4)));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void TablesDefensivelyPreprocessTheirInputArrays()
    {
        double[] aliasInput = [1.0, 3.0, 0.0, 6.0];
        double[] guideInput = [1.0, 3.0, 0.0, 6.0];
        AliasTable actualAlias = new AliasTable(aliasInput);
        AliasTable expectedAlias = new AliasTable([1.0, 3.0, 0.0, 6.0]);
        GuideTable actualGuide = new GuideTable(guideInput);
        GuideTable expectedGuide = new GuideTable([1.0, 3.0, 0.0, 6.0]);
        RandomGenerator actualAliasGenerator = Create(567UL);
        RandomGenerator expectedAliasGenerator = Create(567UL);
        RandomGenerator actualGuideGenerator = Create(678UL);
        RandomGenerator expectedGuideGenerator = Create(678UL);

        aliasInput[0] = double.NaN;
        aliasInput[1] = 0.0;
        aliasInput[3] = double.PositiveInfinity;
        guideInput[0] = 0.0;
        guideInput[1] = double.NegativeInfinity;
        guideInput[3] = 0.0;

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(actualAlias.Sample(actualAliasGenerator), Is.EqualTo(expectedAlias.Sample(expectedAliasGenerator)));
            Assert.That(actualGuide.Sample(actualGuideGenerator), Is.EqualTo(expectedGuide.Sample(expectedGuideGenerator)));
        }
    }

    [Test]
    public void DeterministicTablesAndEmptyFillsConsumeNoState()
    {
        AliasTable alias = new AliasTable([0.0, 0.0, 5.0, 0.0]);
        GuideTable guide = new GuideTable([0.0, 0.0, 5.0, 0.0]);
        RandomGenerator generator = Create(789UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        int[] aliasValues = new int[16];
        int[] guideValues = new int[16];

        int aliasScalar = alias.Sample(generator);
        int guideScalar = guide.Sample(generator);
        alias.Fill(generator, aliasValues);
        guide.Fill(generator, guideValues);
        alias.Fill(generator, Span<int>.Empty);
        guide.Fill(generator, Span<int>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(aliasScalar, Is.EqualTo(2));
            Assert.That(guideScalar, Is.EqualTo(2));
            Assert.That(aliasValues, Is.All.EqualTo(2));
            Assert.That(guideValues, Is.All.EqualTo(2));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void TableFillsMatchRepeatedScalarSamplesExactly()
    {
        AliasTable alias = new AliasTable([5.0, 0.0, 2.0, 3.0]);
        RandomGenerator aliasFillGenerator = Create(890UL);
        RandomGenerator aliasScalarGenerator = Create(890UL);
        int[] aliasActual = new int[257];
        int[] aliasExpected = new int[aliasActual.Length];
        alias.Fill(aliasFillGenerator, aliasActual);
        for (int index = 0; index < aliasExpected.Length; index++)
        {
            aliasExpected[index] = alias.Sample(aliasScalarGenerator);
        }

        GuideTable guide = new GuideTable([5.0, 0.0, 2.0, 3.0]);
        RandomGenerator guideFillGenerator = Create(901UL);
        RandomGenerator guideScalarGenerator = Create(901UL);
        int[] guideActual = new int[257];
        int[] guideExpected = new int[guideActual.Length];
        guide.Fill(guideFillGenerator, guideActual);
        for (int index = 0; index < guideExpected.Length; index++)
        {
            guideExpected[index] = guide.Sample(guideScalarGenerator);
        }

        Assert.Multiple(() =>
        {
            Assert.That(aliasActual, Is.EqualTo(aliasExpected));
            Assert.That(aliasActual, Has.None.EqualTo(1));
            Assert.That(aliasFillGenerator.ExportState().ToBytes(), Is.EqualTo(aliasScalarGenerator.ExportState().ToBytes()));
            Assert.That(guideActual, Is.EqualTo(guideExpected));
            Assert.That(guideActual, Has.None.EqualTo(1));
            Assert.That(guideFillGenerator.ExportState().ToBytes(), Is.EqualTo(guideScalarGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void CountsAndInvalidArgumentsAreReportedConsistently()
    {
        AliasTable alias = new AliasTable([1.0, 2.0, 3.0]);
        GuideTable guide = new GuideTable([1.0, 2.0, 3.0]);
        Assert.Multiple(() =>
        {
            Assert.That(alias.CategoryCount(), Is.EqualTo(3));
            Assert.That(guide.CategoryCount(), Is.EqualTo(3));
        });

        object[] cases =
        [
            new object[] { (TestDelegate)(() => new AliasTable(NullReference.Value<double[]>())), "probabilities" },
            new object[] { (TestDelegate)(() => new GuideTable(NullReference.Value<double[]>())), "probabilities" },
            new object[] { (TestDelegate)(() => new AliasTable([])), "probabilities" },
            new object[] { (TestDelegate)(() => new GuideTable([0.0, 0.0])), "probabilities" },
            new object[] { (TestDelegate)(() => new AliasTable([1.0, -double.Epsilon])), "probabilities" },
            new object[] { (TestDelegate)(() => new GuideTable([1.0, double.NaN])), "probabilities" },
            new object[] { (TestDelegate)(() => new AliasTable([double.PositiveInfinity])), "probabilities" },
            new object[] { (TestDelegate)(() => alias.Sample(NullReference.Value<global::random.IRandomSource>())), "generator" },
            new object[] { (TestDelegate)(() => guide.Sample(NullReference.Value<global::random.IRandomSource>())), "generator" },
            new object[] { (TestDelegate)(() => alias.Fill(NullReference.Value<IRandomSource>(), Span<int>.Empty)), "generator" },
            new object[] { (TestDelegate)(() => guide.Fill(NullReference.Value<IRandomSource>(), Span<int>.Empty)), "generator" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
